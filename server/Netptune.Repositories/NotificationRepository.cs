using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class NotificationRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, Notification, int>(context, connectionFactory), INotificationRepository
{

    public async Task<List<NotificationViewModel>> GetUserNotifications(string userId, int workspaceId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<NotificationViewModel>(new CommandDefinition(
            """
             SELECT
                   n.id
                 , n.is_read         AS isread
                 , n.link
                 , n.entity_type     AS entitytype
                 , n.activity_type   AS activitytype
                 , n.created_at      AS createdat
                 , al.user_id        AS actoruserid
                 , TRIM(u.firstname || ' ' || u.lastname) AS actorusername
                 , u.picture_url     AS actoruserurl
                 , COALESCE(pt.name, p.name, b.name, bg.name) AS entityname
                 , CASE
                     WHEN n.entity_type = @taskType    AND al.project_slug IS NOT NULL THEN al.project_slug || '-' || pt.project_scope_id::text
                     WHEN n.entity_type = @boardType   THEN al.board_slug
                     WHEN n.entity_type = @projectType THEN al.project_slug
                   END AS entityidentifier
             FROM notifications n
             INNER JOIN activity_logs al ON al.id = n.activity_log_id
             INNER JOIN users u ON u.id = al.user_id
             LEFT JOIN project_tasks pt  ON pt.id  = al.task_id         AND n.entity_type = @taskType
             LEFT JOIN projects p        ON p.id   = al.project_id      AND n.entity_type = @projectType
             LEFT JOIN boards b          ON b.id   = al.board_id        AND n.entity_type = @boardType
             LEFT JOIN board_groups bg   ON bg.id  = al.board_group_id  AND n.entity_type = @boardGroupType
             WHERE n.is_deleted = FALSE
               AND n.user_id = @userId
               AND n.workspace_id = @workspaceId
             ORDER BY n.created_at DESC
             LIMIT 50
            """, new {
            userId,
            workspaceId,
            taskType = EntityType.Task,
            projectType = EntityType.Project,
            boardType = EntityType.Board,
            boardGroupType = EntityType.BoardGroup,
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
