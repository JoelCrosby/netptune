using Mediator;

using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.NotificationSubscriptions.Commands;

public sealed record DeleteNotificationSubscriptionCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteNotificationSubscriptionCommandHandler
    : IRequestHandler<DeleteNotificationSubscriptionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly INotificationSubscriptionRepository Subscriptions;
    private readonly IIdentityService Identity;

    public DeleteNotificationSubscriptionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        INotificationSubscriptionRepository subscriptions,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Subscriptions = subscriptions;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteNotificationSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var subscription = await Subscriptions.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (subscription is null || subscription.IsDeleted)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isOwnSubscription = subscription.UserId == userId;

        if (!isOwnSubscription)
        {
            return ClientResponse.NotFound;
        }

        subscription.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
