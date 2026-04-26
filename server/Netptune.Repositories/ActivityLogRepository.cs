using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Audit;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Audit;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class ActivityLogRepository : WorkspaceEntityRepository<DataContext, ActivityLog, int>, IActivityLogRepository
{
    public ActivityLogRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId)
    {
        Expression<Func<ActivityLog, bool>> predicate = entityType switch
        {
            EntityType.Task => x => (x.EntityId == entityId || x.TaskId == entityId),
            EntityType.Board => x => (x.EntityId == entityId || x.BoardId == entityId),
            EntityType.Project => x => (x.EntityId == entityId || x.ProjectId == entityId),
            EntityType.Workspace => x => (x.EntityId == entityId || x.WorkspaceId == entityId),
            EntityType.BoardGroup => x => (x.EntityId == entityId || x.BoardGroupId == entityId),
            _ => _ => true,
        };

        return Entities
            .Where(x => !x.IsDeleted && x.EntityType == entityType)
            .Where(predicate)
            .OrderByDescending(x => x.OccurredAt)
            .Include(x => x.User)
            .Select(y => new ActivityViewModel
            {
                Type = y.Type,
                EntityId = y.EntityId,
                EntityType = entityType,
                UserId = y.UserId,
                UserUsername = y.User.DisplayName,
                UserPictureUrl = y.User.PictureUrl,
                Time = y.OccurredAt,
                Meta = y.Meta,
            })
            .ToReadonlyListAsync(true);
    }

    public async Task<AuditLogPage> GetAuditLog(AuditLogFilter filter)
    {
        var query = BuildAuditQuery(filter);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(x => x.User)
            .Select(y => ToAuditLogViewModel(y))
            .AsNoTracking()
            .ToListAsync();

        return new AuditLogPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public Task<List<AuditLogViewModel>> GetAuditLogForExport(AuditLogFilter filter)
    {
        return BuildAuditQuery(filter)
            .OrderByDescending(x => x.OccurredAt)
            .Include(x => x.User)
            .Select(y => ToAuditLogViewModel(y))
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<List<AuditActivityPoint>> GetActivitySummary(AuditLogFilter filter)
    {
        return BuildAuditQuery(filter)
            .GroupBy(x => x.OccurredAt.Date)
            .Select(g => new AuditActivityPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AnonymiseUser(string userId, int workspaceId)
    {
        // Replace identifying data on audit rows with a placeholder.
        // The log row is preserved for audit trail integrity.
        await Entities
            .Where(x => x.UserId == userId && x.WorkspaceId == workspaceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UserId, "[deleted]"));
    }

    private IQueryable<ActivityLog> BuildAuditQuery(AuditLogFilter filter)
    {
        var query = Entities.Where(x => x.WorkspaceId == filter.WorkspaceId);

        if (filter.UserId is not null)
            query = query.Where(x => x.UserId == filter.UserId);

        if (filter.EntityType is not null)
            query = query.Where(x => x.EntityType == filter.EntityType);

        if (filter.ActivityType is not null)
            query = query.Where(x => x.Type == filter.ActivityType);

        if (filter.From is not null)
            query = query.Where(x => x.OccurredAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(x => x.OccurredAt <= filter.To);

        return query;
    }

    private static AuditLogViewModel ToAuditLogViewModel(ActivityLog y) => new()
    {
        Id = y.Id,
        OccurredAt = y.OccurredAt,
        UserId = y.UserId,
        UserDisplayName = y.User.DisplayName,
        UserPictureUrl = y.User.PictureUrl,
        Type = y.Type,
        EntityType = y.EntityType,
        EntityId = y.EntityId,
        WorkspaceSlug = y.WorkspaceSlug,
        ProjectSlug = y.ProjectSlug,
        BoardSlug = y.BoardSlug,
        Meta = y.Meta,
    };
}
