using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class NotificationRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, Notification, int>(context, connectionFactory), INotificationRepository
{

    public async Task<List<NotificationViewModel>> GetUserNotifications(
        string userId,
        int workspaceId,
        Pagination pagination,
        string? search = null,
        string? actorId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<NotificationViewModel>(new CommandDefinition(
            SqlScripts.GetUserNotifications, new {
            userId,
            workspaceId,
            search = ToSearchParam(search),
            actorId = ToActorParam(actorId),
            skip = pagination.Skip,
            take = pagination.PageSize,
            taskType = EntityType.Task,
            projectType = EntityType.Project,
            boardType = EntityType.Board,
            boardGroupType = EntityType.BoardGroup,
            statusType = EntityType.Status,
        }, cancellationToken: cancellationToken));

        return results.AsList();
    }

    public async Task<int> GetUserNotificationsCount(string userId, int workspaceId, string? search = null, string? actorId = null, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            SqlScripts.GetUserNotificationsCount, new {
            userId,
            workspaceId,
            search = ToSearchParam(search),
            actorId = ToActorParam(actorId),
            taskType = EntityType.Task,
            projectType = EntityType.Project,
            boardType = EntityType.Board,
            boardGroupType = EntityType.BoardGroup,
            statusType = EntityType.Status,
        }, cancellationToken: cancellationToken));
    }

    private static string? ToSearchParam(string? search)
        => string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

    private static string? ToActorParam(string? actorId)
        => string.IsNullOrWhiteSpace(actorId) ? null : actorId;

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

    public async Task<List<int>> MarkAsRead(IEnumerable<int> ids, string userId, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return [];

        var affectedIds = await Entities
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead && idList.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        if (affectedIds.Count == 0) return affectedIds;

        await Entities
            .Where(n => affectedIds.Contains(n.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

        return affectedIds;
    }

    public async Task<List<int>> SoftDelete(IEnumerable<int> ids, string userId, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return [];

        var affectedIds = await Entities
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted && idList.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        if (affectedIds.Count == 0) return affectedIds;

        await Entities
            .Where(n => affectedIds.Contains(n.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsDeleted, true)
                .SetProperty(n => n.DeletedByUserId, userId)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), cancellationToken);

        return affectedIds;
    }
}
