using System.Linq.Expressions;
using System.Text.Json;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
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
    private sealed record EventSubject(int WorkspaceId, string SubjectType, string SubjectId);

    public EventRecordRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<EventRecord> AppendAsync(EventRecord record, bool publish, CancellationToken cancellationToken = default)
    {
        var hasIdentifiableSubject = record.WorkspaceId.HasValue &&
            record.SubjectType is not null &&
            record.SubjectId is not null;

        if (hasIdentifiableSubject)
        {
            var subject = new EventSubject(
                record.WorkspaceId.GetValueOrDefault(),
                record.SubjectType!,
                record.SubjectId!);
            record.SubjectSequence = await AllocateSubjectSequence(subject, cancellationToken);
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

    private async Task<long> AllocateSubjectSequence(EventSubject subject, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO event_stream_heads (workspace_id, subject_type, subject_id, current_sequence)
            VALUES (@WorkspaceId, @SubjectType, @SubjectId, 1)
            ON CONFLICT (workspace_id, subject_type, subject_id)
            DO UPDATE SET current_sequence = event_stream_heads.current_sequence + 1
            RETURNING current_sequence;
            """;
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(sql, subject, cancellationToken: cancellationToken);
        var sequence = await connection.ExecuteScalarAsync<long?>(command);

        if (!sequence.HasValue)
        {
            throw new InvalidOperationException("The event subject sequence could not be allocated.");
        }

        return sequence.Value;
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
            EntityType.Task => x => x.EntityId == entityId || x.TaskId == entityId,
            EntityType.Board => x => x.EntityId == entityId || x.BoardId == entityId,
            EntityType.Project => x => x.EntityId == entityId || x.ProjectId == entityId,
            EntityType.Workspace => x => x.EntityId == entityId || x.WorkspaceId == entityId,
            EntityType.BoardGroup => x => x.EntityId == entityId || x.BoardGroupId == entityId,
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
                UserIsServiceAccount = y.User.UserType == AppUserType.ServiceAccount,
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
            query = ApplyActivityTypeFilter(query, filter.ActivityType.Value);
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

    private static IQueryable<EventRecord> ApplyActivityTypeFilter(
        IQueryable<EventRecord> query,
        ActivityType activityType)
    {
        var legacyActivityType = (int)activityType;

        return activityType switch
        {
            ActivityType.Create => ApplyCreateActivityFilter(query, legacyActivityType),
            ActivityType.ModifyStatus => ApplyStatusModificationFilter(query, legacyActivityType),
            ActivityType.ModifyEstimate => ApplyEstimateModificationFilter(query, legacyActivityType),
            ActivityType.Assign => ApplyAssignmentFilter(query, legacyActivityType),
            ActivityType.Unassign => ApplyUnassignmentFilter(query, legacyActivityType),
            _ => ApplyLegacyActivityFilter(query, legacyActivityType),
        };
    }

    private static IQueryable<EventRecord> ApplyCreateActivityFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            record.EventKey == EventKeys.EntityCreated ||
            (record.EventKey == EventKeys.EntityActivityRecorded &&
                record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
    }

    private static IQueryable<EventRecord> ApplyStatusModificationFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            record.EventKey == EventKeys.ScopeLifecycleTransitioned ||
            (record.EventKey == EventKeys.EntityFieldTransitioned && record.Payload.RootElement.GetProperty("field").GetString() == "status") ||
            (record.EventKey == EventKeys.EntityActivityRecorded && record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
    }

    private static IQueryable<EventRecord> ApplyEstimateModificationFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            record.EventKey == EventKeys.ScopeMemberAttributeChanged ||
            (record.EventKey == EventKeys.EntityFieldTransitioned && record.Payload.RootElement.GetProperty("field").GetString() == "estimate") ||
            (record.EventKey == EventKeys.EntityActivityRecorded && record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
    }

    private static IQueryable<EventRecord> ApplyAssignmentFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            (record.EventKey == EventKeys.ScopeMemberChanged && record.Payload.RootElement.GetProperty("change").GetString() != "removed") ||
            (record.EventKey == EventKeys.EntityActivityRecorded && record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
    }

    private static IQueryable<EventRecord> ApplyUnassignmentFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            (record.EventKey == EventKeys.ScopeMemberChanged && record.Payload.RootElement.GetProperty("change").GetString() == "removed") ||
            (record.EventKey == EventKeys.EntityActivityRecorded && record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
    }

    private static IQueryable<EventRecord> ApplyLegacyActivityFilter(
        IQueryable<EventRecord> query,
        int legacyActivityType)
    {
        return query.Where(record =>
            record.EventKey == EventKeys.EntityActivityRecorded &&
            record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType);
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
            var hasLegacyActivityType = payload.TryGetProperty("activityType", out var typeValue);
            var activityType = hasLegacyActivityType
                ? (ActivityType)typeValue.GetInt32()
                : EventKeys.ActivityTypeFor(record.EventKey, payload);
            var hasKnownEntityType = EventEntityTypes.TryParse(record.SubjectType, out var parsedEntityType);
            var entityType = hasKnownEntityType
                ? parsedEntityType
                : EntityType.Workspace;
            var actorIsUnknown = actor is null;
            var actorHasNoDisplayName = !actorIsUnknown &&
                string.IsNullOrEmpty(actor!.Firstname) &&
                string.IsNullOrEmpty(actor.Lastname);
            string actorDisplayName;

            if (actorIsUnknown)
            {
                actorDisplayName = "System";
            }
            else if (actorHasNoDisplayName)
            {
                actorDisplayName = actor!.UserName!;
            }
            else
            {
                actorDisplayName = actor!.Firstname + " " + actor.Lastname;
            }

            var hasNumericEntityId = int.TryParse(record.SubjectId, out var entityId);

            return new AuditLogViewModel
            {
                Id = record.Id,
                OccurredAt = record.OccurredAt,
                UserId = record.ActorUserId,
                UserDisplayName = actorDisplayName,
                UserPictureUrl = actor?.PictureUrl,
                Type = activityType,
                EntityType = entityType,
                EntityId = hasNumericEntityId ? entityId : null,
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
