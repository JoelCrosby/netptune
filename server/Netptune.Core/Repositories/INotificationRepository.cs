using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;

namespace Netptune.Core.Repositories;

public interface INotificationRepository : IRepository<Notification, int>
{
    Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId, string? search = null, string? actorId = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    Task<int> GetUserNotificationsCount(string userId, int workspaceId, string? search = null, string? actorId = null, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCount(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task MarkAllAsRead(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<int>> MarkAsRead(IEnumerable<int> ids, string userId, CancellationToken cancellationToken = default);

    Task<List<int>> SoftDelete(IEnumerable<int> ids, string userId, CancellationToken cancellationToken = default);
}
