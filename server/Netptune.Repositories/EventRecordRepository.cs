using System.Linq.Expressions;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Audit;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Audit;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class EventRecordRepository : Repository<DataContext, EventRecord, long>, IEventRecordRepository
{
    public EventRecordRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<EventRecord> AppendAsync(
        EventRecord record,
        bool publish,
        CancellationToken cancellationToken = default)
    {

        if (record.WorkspaceId.HasValue && record.SubjectType is not null && record.SubjectId is not null)
        {
            record.SubjectSequence = await AllocateSubjectSequence(
                record.WorkspaceId.Value,
                record.SubjectType,
                record.SubjectId,
                cancellationToken);
        }

        await Entities.AddAsync(record, cancellationToken);

        if (publish)
        {
            Context.EventOutbox.Add(new EventOutbox
            {
                EventRecord = record,
                AvailableAt = DateTime.UtcNow,
            });
        }

        return record;
    }

    private async Task<long> AllocateSubjectSequence(
        int workspaceId,
        string subjectType,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)Context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;
        command.CommandText =
            """
            INSERT INTO event_stream_heads (workspace_id, subject_type, subject_id, current_sequence)
            VALUES (@workspace_id, @subject_type, @subject_id, 1)
            ON CONFLICT (workspace_id, subject_type, subject_id)
            DO UPDATE SET current_sequence = event_stream_heads.current_sequence + 1
            RETURNING current_sequence;
            """;
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("subject_type", subjectType);
        command.Parameters.AddWithValue("subject_id", subjectId);

        var sequence = await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("The event subject sequence could not be allocated.");

        return (long)sequence;
    }

    public async Task<List<ActivityViewModel>> GetActivities(
        EntityType entityType,
        int entityId,
        CancellationToken cancellationToken = default,
        int? take = null,
        string? cursor = null)
    {
        var limit = Math.Clamp(take ?? PaginationDefaults.DefaultPageSize, 1, PaginationDefaults.MaxPageSize);

        Expression<Func<ActivityEntry, bool>> predicate = entityType switch
        {
            EntityType.Task => x => (x.EntityId == entityId || x.TaskId == entityId),
            EntityType.Board => x => (x.EntityId == entityId || x.BoardId == entityId),
            EntityType.Project => x => (x.EntityId == entityId || x.ProjectId == entityId),
            EntityType.Workspace => x => (x.EntityId == entityId || x.WorkspaceId == entityId),
            EntityType.BoardGroup => x => (x.EntityId == entityId || x.BoardGroupId == entityId),
            EntityType.Sprint => x => x.EntityId == entityId,
            EntityType.Status => x => x.EntityId == entityId,
            _ => _ => true,
        };

        var cursorRequest = new CursorRequest { Cursor = cursor };
        var hasCursor = cursorRequest.TryGetCursor(out var cursorOccurredAt, out var cursorId);

        var query = Context.Set<ActivityEntry>()
            .Where(x => !x.IsDeleted && x.EntityType == entityType)
            .Where(predicate);

        if (hasCursor)
        {
            query = query.Where(x => x.LastOccurredAt < cursorOccurredAt || (x.LastOccurredAt == cursorOccurredAt && x.Id < cursorId));
        }

        var activities = await query
            .OrderByDescending(x => x.LastOccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(y => new ActivityViewModel
            {
                Id = y.Id,
                Type = y.ActivityType,
                EntityId = y.EntityId,
                EntityType = entityType,
                UserId = y.UserId,
                UserUsername = string.IsNullOrEmpty(y.User.Firstname) && string.IsNullOrEmpty(y.User.Lastname)
                    ? y.User.UserName!
                    : y.User.Firstname + " " + y.User.Lastname,
                UserPictureUrl = y.User.PictureUrl,
                Time = y.LastOccurredAt,
                FirstTime = y.FirstOccurredAt,
                RevisionCount = y.RevisionCount,
                ChangedFields = y.ChangedFields,
                Meta = y.Meta,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return activities;
    }

    public async Task<HashSet<Guid>> GetExistingEventIds(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        var ids = eventIds.ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var existing = await Entities
            .AsNoTracking()
            .Where(x => ids.Contains(x.EventId))
            .Select(x => x.EventId)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }

    public async Task<AuditLogPage> GetAuditLog(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildAuditQuery(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = await ToAuditViewModels(records, cancellationToken);

        return new AuditLogPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<List<AuditLogViewModel>> GetAuditLogForExport(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await BuildAuditQuery(filter)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(PaginationDefaults.MaxExportRows)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var auditLogs = await ToAuditViewModels(records, cancellationToken);

        return auditLogs;
    }

    public Task<List<AuditActivityPoint>> GetActivitySummary(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        return BuildAuditQuery(filter)
            .GroupBy(x => x.OccurredAt.Date)
            .Select(g => new AuditActivityPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<EventRecord> BuildAuditQuery(AuditLogFilter filter)
    {
        var query = Entities.Where(x => x.WorkspaceId == filter.WorkspaceId);

        if (filter.UserId is not null)
        {
            query = query.Where(x => x.ActorUserId == filter.UserId);
        }

        if (filter.EntityType is not null)
        {
            var subjectType = EventEntityTypes.From(filter.EntityType.Value);
            query = query.Where(x => x.SubjectType == subjectType);
        }

        if (filter.ActivityType is not null)
        {
            var activityType = (int)filter.ActivityType.Value;
            query = query.Where(x =>
                x.Payload.RootElement.GetProperty("activityType").GetInt32() == activityType);
        }

        if (filter.From is not null)
        {
            query = query.Where(x => x.OccurredAt >= filter.From);
        }

        if (filter.To is not null)
        {
            query = query.Where(x => x.OccurredAt <= filter.To);
        }

        return query;
    }

    private async Task<List<AuditLogViewModel>> ToAuditViewModels(
        IReadOnlyCollection<EventRecord> records,
        CancellationToken cancellationToken)
    {
        var actorIds = records
            .Where(record => record.ActorUserId is not null)
            .Select(record => record.ActorUserId!)
            .Distinct()
            .ToList();

        var actors = await Context.AppUsers
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return records.Select(record =>
        {
            actors.TryGetValue(record.ActorUserId ?? string.Empty, out var actor);
            var payload = record.Payload.RootElement;
            var activityType = payload.TryGetProperty("activityType", out var typeValue)
                ? (ActivityType)typeValue.GetInt32()
                : ActivityType.Modify;
            var entityType = EventEntityTypes.TryParse(record.SubjectType, out var parsedEntityType)
                ? parsedEntityType
                : EntityType.Workspace;

            return new AuditLogViewModel
            {
                Id = record.Id,
                OccurredAt = record.OccurredAt,
                UserId = record.ActorUserId,
                UserDisplayName = actor is null
                    ? "System"
                    : string.IsNullOrEmpty(actor.Firstname) && string.IsNullOrEmpty(actor.Lastname)
                        ? actor.UserName!
                        : actor.Firstname + " " + actor.Lastname,
                UserPictureUrl = actor?.PictureUrl,
                Type = activityType,
                EntityType = entityType,
                EntityId = int.TryParse(record.SubjectId, out var entityId) ? entityId : null,
                WorkspaceSlug = GetPayloadString(payload, "workspaceSlug"),
                ProjectSlug = GetPayloadString(payload, "projectSlug"),
                BoardSlug = GetPayloadString(payload, "boardSlug"),
                Meta = JsonDocument.Parse(record.Payload.RootElement.GetRawText()),
            };
        }).ToList();
    }

    private static string? GetPayloadString(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

}
