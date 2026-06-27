using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Handlers.Notifications.Queries;

public sealed record GetUserNotificationsQuery(int Skip = 0, int Take = 50) : IRequest<ClientResponse<List<NotificationViewModel>>>;

public sealed class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, ClientResponse<List<NotificationViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUserNotificationsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<NotificationViewModel>>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var skip = Math.Max(request.Skip, 0);
        var take = Math.Clamp(request.Take, 1, 100);

        var notifications = await UnitOfWork.Notifications.GetUserNotifications(userId, workspaceId, skip, take, cancellationToken);

        return ClientResponse<List<NotificationViewModel>>.Success(notifications);
    }
}
