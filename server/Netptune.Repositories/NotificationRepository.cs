using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class NotificationRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, Notification, int>(context, connectionFactory), INotificationRepository
{

    public async Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<NotificationViewModel>(new CommandDefinition(
            SqlScripts.GetUserNotifications, new {
            userId,
            workspaceId,
            skip,
            take,
            taskType = EntityType.Task,
            projectType = EntityType.Project,
            boardType = EntityType.Board,
            boardGroupType = EntityType.BoardGroup,
            statusType = EntityType.Status,
        }, cancellationToken: cancellationToken));

        return results.AsList();
    }

    public Task<int> GetUnreadCount(string userId, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .CountAsync(n => !n.IsDeleted && n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAllAsRead(string userId, int workspaceId, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(n => n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
