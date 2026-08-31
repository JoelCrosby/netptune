using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Handlers.NotificationSubscriptions.Commands;

public sealed record UpsertNotificationSubscriptionRequest
{
    public required NotificationScope Scope { get; init; }

    public required int ScopeEntityId { get; init; }

    public required NotificationSubscriptionEvents Events { get; init; }
}

public sealed record UpsertNotificationSubscriptionCommand(UpsertNotificationSubscriptionRequest Request)
    : IRequest<ClientResponse<NotificationSubscriptionViewModel>>;

public sealed class UpsertNotificationSubscriptionCommandHandler
    : IRequestHandler<UpsertNotificationSubscriptionCommand, ClientResponse<NotificationSubscriptionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly INotificationSubscriptionRepository Subscriptions;
    private readonly IIdentityService Identity;

    public UpsertNotificationSubscriptionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        INotificationSubscriptionRepository subscriptions,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Subscriptions = subscriptions;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<NotificationSubscriptionViewModel>> Handle(
        UpsertNotificationSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Request;
        var isKnownScope = Enum.IsDefined(input.Scope);

        if (!isKnownScope)
        {
            return ClientResponse<NotificationSubscriptionViewModel>.Failed("That is not a scope you can subscribe to.");
        }

        var hasEvents = input.Events != NotificationSubscriptionEvents.None;

        if (!hasEvents)
        {
            return ClientResponse<NotificationSubscriptionViewModel>.Failed("Choose at least one event to be notified about.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var scopeExists = await ScopeExists(input.Scope, input.ScopeEntityId, workspaceId, cancellationToken);

        if (!scopeExists)
        {
            return ClientResponse<NotificationSubscriptionViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var subscription = await Resolve(input, workspaceId, userId, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var workspaceKey = Identity.GetWorkspaceKey();
        var viewModels = await Subscriptions.GetViewModelsForUser(workspaceId, userId, workspaceKey, cancellationToken);
        var viewModel = viewModels.SingleOrDefault(candidate => candidate.Id == subscription.Id);

        if (viewModel is null)
        {
            return ClientResponse<NotificationSubscriptionViewModel>.NotFound;
        }

        return ClientResponse<NotificationSubscriptionViewModel>.Success(viewModel);
    }

    // A soft-deleted subscription is revived rather than replaced: the unique index only covers live
    // rows, so blind inserts would pile up tombstoned duplicates.
    private async Task<NotificationSubscription> Resolve(
        UpsertNotificationSubscriptionRequest input,
        int workspaceId,
        string userId,
        CancellationToken cancellationToken)
    {
        var existing = await Subscriptions.Find(workspaceId, userId, input.Scope, input.ScopeEntityId, cancellationToken);

        if (existing is not null)
        {
            existing.Events = input.Events;
            existing.IsDeleted = false;
            existing.DeletedByUserId = null;
            existing.ModifiedByUserId = userId;

            return existing;
        }

        var subscription = new NotificationSubscription
        {
            UserId = userId,
            Scope = input.Scope,
            ScopeEntityId = input.ScopeEntityId,
            Events = input.Events,
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            OwnerId = userId,
        };

        return await Subscriptions.AddAsync(subscription, cancellationToken);
    }

    private async Task<bool> ScopeExists(
        NotificationScope scope,
        int scopeEntityId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (scope is NotificationScope.Project)
        {
            var project = await UnitOfWork.Projects.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

            return project is not null && !project.IsDeleted;
        }

        if (scope is NotificationScope.Board)
        {
            var board = await UnitOfWork.Boards.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

            return board is not null && !board.IsDeleted;
        }

        if (scope is NotificationScope.BoardGroup)
        {
            var boardGroup = await UnitOfWork.BoardGroups.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

            if (boardGroup is null || boardGroup.IsDeleted)
            {
                return false;
            }

            // Deleting a board leaves its groups alone, so a group can outlive the board it hangs
            // off. One of those can never be reached by a task event, and a subscription to it
            // would be invisible to the list the settings screen reads.
            var groupBoard = await UnitOfWork.Boards.GetInWorkspace(boardGroup.BoardId, workspaceId, true, cancellationToken);

            return groupBoard is not null && !groupBoard.IsDeleted;
        }

        var sprint = await UnitOfWork.Sprints.GetInWorkspace(scopeEntityId, workspaceId, true, cancellationToken);

        return sprint is not null && !sprint.IsDeleted;
    }
}
