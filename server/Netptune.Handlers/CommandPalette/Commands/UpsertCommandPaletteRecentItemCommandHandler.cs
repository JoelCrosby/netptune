using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.CommandPalette.Queries;

namespace Netptune.Handlers.CommandPalette.Commands;

public sealed record UpsertCommandPaletteRecentItemCommand(
    UpsertCommandPaletteRecentItemRequest RecentItem) : IRequest<ClientResponse<CommandPaletteRecentItemsResponse>>;

public sealed class UpsertCommandPaletteRecentItemCommandHandler
    : IRequestHandler<UpsertCommandPaletteRecentItemCommand, ClientResponse<CommandPaletteRecentItemsResponse>>
{
    private const int MaxRecentItems = 10;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IMediator Mediator;

    public UpsertCommandPaletteRecentItemCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Mediator = mediator;
    }

    public async ValueTask<ClientResponse<CommandPaletteRecentItemsResponse>> Handle(
        UpsertCommandPaletteRecentItemCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecentItem.Type) ||
            string.IsNullOrWhiteSpace(request.RecentItem.Title) ||
            string.IsNullOrWhiteSpace(request.RecentItem.Url))
        {
            return ClientResponse<CommandPaletteRecentItemsResponse>.Failed("Type, title, and URL are required.");
        }

        var workspaceId = await GetRequiredWorkspaceId();
        if (workspaceId is null)
        {
            return ClientResponse<CommandPaletteRecentItemsResponse>.Failed("A workspace is required.");
        }

        var userId = Identity.GetCurrentUserId();
        var existing = await UnitOfWork.CommandPaletteRecentItems.GetByUrl(
            userId,
            workspaceId.Value,
            request.RecentItem.Url,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (existing is null)
        {
            await UnitOfWork.CommandPaletteRecentItems.AddAsync(new CommandPaletteRecentItem
            {
                UserId = userId,
                WorkspaceId = workspaceId.Value,
                Type = request.RecentItem.Type,
                EntityId = request.RecentItem.EntityId,
                Title = request.RecentItem.Title,
                Url = request.RecentItem.Url,
                LastAccessedAt = now,
            }, cancellationToken);
        }
        else
        {
            existing.Type = request.RecentItem.Type;
            existing.EntityId = request.RecentItem.EntityId;
            existing.Title = request.RecentItem.Title;
            existing.LastAccessedAt = now;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
        await PruneWorkspace(userId, workspaceId.Value, cancellationToken);

        return await Mediator.Send(new GetCommandPaletteRecentItemsQuery(), cancellationToken);
    }

    private async Task PruneWorkspace(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var staleItems = await UnitOfWork.CommandPaletteRecentItems.GetStaleWorkspaceItems(
            userId,
            workspaceId,
            MaxRecentItems,
            cancellationToken);

        if (staleItems.Count == 0) return;

        await UnitOfWork.CommandPaletteRecentItems.DeletePermanent(staleItems, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task<int?> GetRequiredWorkspaceId()
    {
        return Identity.TryGetWorkspaceKey() is null ? null : await Identity.GetWorkspaceId();
    }
}
