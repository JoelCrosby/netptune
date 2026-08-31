using Mediator;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Handlers.NotificationSubscriptions.Queries;

public sealed record GetNotificationSubscriptionsQuery : IRequest<List<NotificationSubscriptionViewModel>>;

public sealed class GetNotificationSubscriptionsQueryHandler
    : IRequestHandler<GetNotificationSubscriptionsQuery, List<NotificationSubscriptionViewModel>>
{
    private readonly INotificationSubscriptionRepository Subscriptions;
    private readonly IIdentityService Identity;

    public GetNotificationSubscriptionsQueryHandler(
        INotificationSubscriptionRepository subscriptions,
        IIdentityService identity)
    {
        Subscriptions = subscriptions;
        Identity = identity;
    }

    public async ValueTask<List<NotificationSubscriptionViewModel>> Handle(
        GetNotificationSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var userId = Identity.GetCurrentUserId();

        var subscriptions = await Subscriptions.GetViewModelsForUser(
            workspaceId,
            userId,
            workspaceKey,
            cancellationToken);

        return subscriptions;
    }
}
