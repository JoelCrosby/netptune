using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class NotificationRepository : Repository<DataContext, Notification, int>, INotificationRepository
{
    public NotificationRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId)
    {
        return Entities
            .Where(n => !n.IsDeleted && n.UserId == userId && n.WorkspaceId == workspaceId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationViewModel
            {
                Id = n.Id,
                IsRead = n.IsRead,
                Link = n.Link,
                EntityType = n.EntityType,
                ActivityType = n.ActivityType,
                CreatedAt = n.CreatedAt,
                ActorUserId = n.ActivityLog.UserId,
                ActorUsername = n.ActivityLog.User.DisplayName,
                ActorPictureUrl = n.ActivityLog.User.PictureUrl,
            })
            .ToListAsync();
    }

    public Task<int> GetUnreadCount(string userId, int workspaceId)
    {
        return Entities
            .CountAsync(n => !n.IsDeleted && n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead);
    }

    public async Task MarkAsRead(int id, string userId)
    {
        var notification = await Entities
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification is not null)
        {
            notification.IsRead = true;
        }
    }

    public async Task MarkAllAsRead(string userId, int workspaceId)
    {
        await Entities
            .Where(n => n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
