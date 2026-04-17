using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Core.Services;

public interface INotificationService
{
    Task<ClientResponse<List<NotificationViewModel>>> GetUserNotifications();

    Task<ClientResponse<int>> GetUnreadCount();

    Task<ClientResponse> MarkAsRead(int id);

    Task<ClientResponse> MarkAllAsRead();
}
