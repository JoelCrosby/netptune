using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Services.Notifications.Queries;

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
        var notifications = await UnitOfWork.Notifications.GetUserNotifications(userId, workspaceId);

        return ClientResponse<List<NotificationViewModel>>.Success(notifications);
    }
}
