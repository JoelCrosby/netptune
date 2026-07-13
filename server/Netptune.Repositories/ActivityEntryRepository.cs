using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using NpgsqlTypes;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class ActivityEntryRepository : WorkspaceEntityRepository<DataContext, ActivityEntry, int>, IActivityEntryRepository
{
    public ActivityEntryRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public static Expression<Func<ActivityEntry, bool>> MergeCandidatePredicate(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now)
    {
        return entry =>
            entry.WorkspaceId == workspaceId
            && entry.EntityType == entityType
            && entry.EntityId == entityId
            && entry.UserId == userId
            && entry.IsOpen
            && !entry.IsDeleted
            && entry.WindowExpiresAt > now;
    }

    public static Expression<Func<ActivityEntry, bool>> OtherUsersOpenEntriesPredicate(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now)
    {
        return entry =>
            entry.WorkspaceId == workspaceId
            && entry.EntityType == entityType
            && entry.EntityId == entityId
            && entry.UserId != userId
            && entry.IsOpen
            && entry.WindowExpiresAt > now;
    }

    public Task<ActivityEntry?> FindMergeCandidate(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(MergeCandidatePredicate(workspaceId, entityType, entityId, userId, now))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> ExpireEntriesForOtherUsers(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(OtherUsersOpenEntriesPredicate(workspaceId, entityType, entityId, userId, now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entry => entry.WindowExpiresAt, now)
                .SetProperty(entry => entry.UpdatedAt, now), cancellationToken);
    }

    public static IQueryable<ActivityEntry> UpsertQuery(
        DbSet<ActivityEntry> entries,
        ActivityEntryUpsert upsert,
        DateTime now,
        TimeSpan windowDuration,
        TimeSpan maxWindowDuration)
    {
        return entries.FromSqlRaw(
            ActivityEntryScripts.UpsertActivityEntry,
            Value("workspace_id", upsert.WorkspaceId),
            Value("workspace_slug", upsert.WorkspaceSlug),
            Value("entity_type", (int) upsert.EntityType),
            Value("entity_id", upsert.EntityId),
            Value("user_id", upsert.UserId),
            Value("activity_type", (int) upsert.ActivityType),
            Value("changed_fields", upsert.ChangedFields),
            Value("meta", upsert.MetaJson),
            Value("last_activity_log_id", upsert.LastActivityLogId),
            Timestamp("first_occurred_at", upsert.FirstOccurredAt),
            Timestamp("last_occurred_at", upsert.LastOccurredAt),
            Value("revision_count", upsert.RevisionCount),
            Value("project_id", upsert.ProjectId),
            Value("project_slug", upsert.ProjectSlug),
            Value("board_id", upsert.BoardId),
            Value("board_slug", upsert.BoardSlug),
            Value("board_group_id", upsert.BoardGroupId),
            Value("task_id", upsert.TaskId),
            Timestamp("now", now),
            Value("window_seconds", windowDuration.TotalSeconds),
            Value("max_window_seconds", maxWindowDuration.TotalSeconds),
            Value("merged_activity_type", (int) ActivityType.Modify));
    }

    public static IQueryable<ActivityEntry> ClaimQuery(DbSet<ActivityEntry> entries, int limit)
    {
        return entries.FromSqlRaw(ActivityEntryScripts.CloseExpiredActivityEntries, Value("limit", limit));
    }

    public async Task<UpsertEntryResult> UpsertEntry(
        ActivityEntryUpsert upsert,
        DateTime now,
        TimeSpan windowDuration,
        TimeSpan maxWindowDuration,
        CancellationToken cancellationToken = default)
    {
        var entries = await UpsertQuery(Entities, upsert, now, windowDuration, maxWindowDuration)
            .ToListAsync(cancellationToken);

        return entries.FirstOrDefault() is { } entry
            ? new UpsertEntryResult.Upserted(entry)
            : new UpsertEntryResult.SlotHeldByStaleEntry();
    }

    private static NpgsqlParameter Value(string name, object? value)
    {
        return new (name, value ?? DBNull.Value);
    }

    private static NpgsqlParameter Timestamp(string name, DateTime value)
    {
        return new (name, NpgsqlDbType.TimestampTz) { Value = value };
    }

    public Task<int> CloseStaleEntry(
        int workspaceId,
        EntityType entityType,
        int entityId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entry =>
                entry.WorkspaceId == workspaceId
                && entry.EntityType == entityType
                && entry.EntityId == entityId
                && entry.UserId == userId
                && entry.IsOpen
                && !entry.IsDeleted
                && entry.WindowExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entry => entry.IsOpen, false)
                .SetProperty(entry => entry.UpdatedAt, now), cancellationToken);
    }

    public Task<List<ActivityEntry>> ClaimExpiredEntries(int limit, CancellationToken cancellationToken = default)
    {
        return ClaimQuery(Entities, limit).ToListAsync(cancellationToken);
    }
}
