using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Core.Repositories;

public interface INotificationRepository : IRepository<Notification, int>
{
    Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId);

    Task<int> GetUnreadCount(string userId, int workspaceId);

    Task MarkAsRead(int id, string userId);

    Task MarkAllAsRead(string userId, int workspaceId);
}
