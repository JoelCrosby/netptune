using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Core.Repositories;

public interface INotificationRepository : IRepository<Notification, int>
{
    Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCount(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task MarkAllAsRead(string userId, int workspaceId, CancellationToken cancellationToken = default);
}
