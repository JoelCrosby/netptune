using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Services;

public class NotificationService : INotificationService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public NotificationService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async Task<ClientResponse<List<NotificationViewModel>>> GetUserNotifications()
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var notifications = await UnitOfWork.Notifications.GetUserNotifications(userId, workspaceId);

        return ClientResponse<List<NotificationViewModel>>.Success(notifications);
    }

    public async Task<ClientResponse<int>> GetUnreadCount()
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var count = await UnitOfWork.Notifications.GetUnreadCount(userId, workspaceId);

        return ClientResponse<int>.Success(count);
    }

    public async Task<ClientResponse> MarkAsRead(int id)
    {
        var userId = Identity.GetCurrentUserId();
        var notification = await UnitOfWork.Notifications.GetAsync(id);

        if (notification is null || notification.UserId != userId)
        {
            return ClientResponse.NotFound;
        }

        notification.IsRead = true;

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success;
    }

    public async Task<ClientResponse> MarkAllAsRead()
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        await UnitOfWork.Notifications.MarkAllAsRead(userId, workspaceId);

        return ClientResponse.Success;
    }
}
