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
using Netptune.Core.Responses.Common;
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

    public async Task<PagedResponse<AuditLogViewModel>> GetAuditLog(
        int workspaceId,
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAuditQuery(workspaceId, filter);
        var pagination = filter.GetPagination(PaginationDefaults.MaxAdminPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = await ToAuditViewModels(records, cancellationToken);

        return new PagedResponse<AuditLogViewModel>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<AuditLogDetailViewModel?> GetAuditLogDetail(
        int workspaceId,
        long id,
        CancellationToken cancellationToken = default)
    {
        var record = await Entities
            .AsNoTracking()
            .Include(eventRecord => eventRecord.References)
            .SingleOrDefaultAsync(
                eventRecord => eventRecord.Id == id && eventRecord.WorkspaceId == workspaceId,
                cancellationToken);

        if (record is null)
        {
            return null;
        }

        var auditLog = (await ToAuditViewModels([record], cancellationToken)).Single();

        return new AuditLogDetailViewModel
        {
            Id = auditLog.Id,
            OccurredAt = auditLog.OccurredAt,
            UserId = auditLog.UserId,
            UserDisplayName = auditLog.UserDisplayName,
            UserPictureUrl = auditLog.UserPictureUrl,
            Type = auditLog.Type,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            WorkspaceSlug = auditLog.WorkspaceSlug,
            ProjectSlug = auditLog.ProjectSlug,
            BoardSlug = auditLog.BoardSlug,
            Summary = auditLog.Summary,
            Meta = auditLog.Meta,
            EventId = record.EventId,
            EventKey = record.EventKey,
            SchemaVersion = record.SchemaVersion,
            SubjectType = record.SubjectType,
            SubjectId = record.SubjectId,
            SubjectSequence = record.SubjectSequence,
            RecordedAt = record.RecordedAt,
            CorrelationId = record.CorrelationId,
            CausationEventId = record.CausationEventId,
            IpAddress = record.IpAddress?.ToString(),
            UserAgent = record.UserAgent,
            RetentionClass = record.RetentionClass,
            References = record.References
                .OrderBy(reference => reference.Role)
                .ThenBy(reference => reference.EntityType)
                .ThenBy(reference => reference.EntityId)
                .Select(reference => new AuditLogReferenceViewModel(
                    reference.Role,
                    reference.EntityType,
                    reference.EntityId))
                .ToList(),
        };
    }

    public async Task<List<AuditLogViewModel>> GetAuditLogForExport(
        int workspaceId,
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var records = await BuildAuditQuery(workspaceId, filter)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(PaginationDefaults.MaxExportRows)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var auditLogs = await ToAuditViewModels(records, cancellationToken);

        return auditLogs;
    }

    public Task<List<AuditActivityPoint>> GetActivitySummary(
        int workspaceId,
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        return BuildAuditQuery(workspaceId, filter)
            .GroupBy(x => x.OccurredAt.Date)
            .Select(g => new AuditActivityPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<EventRecord> BuildAuditQuery(int workspaceId, AuditLogFilter filter)
    {
        var query = Entities.Where(x => x.WorkspaceId == workspaceId);

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
            ActivityType.LoginSuccess => ApplyCanonicalActivityFilter(query, EventKeys.SecurityLoginSucceeded, legacyActivityType),
            ActivityType.LoginFailed => ApplyCanonicalActivityFilter(query, EventKeys.SecurityLoginFailed, legacyActivityType),
            ActivityType.ExportRequested => ApplyCanonicalActivityFilter(query, EventKeys.ExportRequested, legacyActivityType),
            ActivityType.RoleChanged => ApplyCanonicalActivityFilter(query, EventKeys.WorkspaceRoleChanged, legacyActivityType),
            ActivityType.WorkspaceSettingsChanged => ApplyCanonicalActivityFilter(query, EventKeys.WorkspaceSettingsChanged, legacyActivityType),
            _ => ApplyLegacyActivityFilter(query, legacyActivityType),
        };
    }

    private static IQueryable<EventRecord> ApplyCanonicalActivityFilter(
        IQueryable<EventRecord> query,
        string eventKey,
        int legacyActivityType)
    {
        return query.Where(record =>
            record.EventKey == eventKey ||
            (record.EventKey == EventKeys.EntityActivityRecorded &&
                record.Payload.RootElement.GetProperty("activityType").GetInt32() == legacyActivityType));
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
        var statusNames = await GetStatusNames(records, cancellationToken);

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
                Summary = BuildSummary(record, activityType, statusNames),
                Meta = JsonDocument.Parse(record.Payload.RootElement.GetRawText()),
            };
        }).ToList();
    }

    private async Task<Dictionary<int, string>> GetStatusNames(
        IReadOnlyCollection<EventRecord> records,
        CancellationToken cancellationToken)
    {
        var statusIds = records
            .SelectMany(GetStatusIds)
            .Distinct()
            .ToList();
        var workspaceIds = records
            .Where(record => record.WorkspaceId.HasValue)
            .Select(record => record.WorkspaceId!.Value)
            .Distinct()
            .ToList();

        if (statusIds.Count == 0 || workspaceIds.Count == 0)
        {
            return [];
        }

        return await Context.Statuses
            .AsNoTracking()
            .Where(status => statusIds.Contains(status.Id) && workspaceIds.Contains(status.WorkspaceId))
            .ToDictionaryAsync(status => status.Id, status => status.Name, cancellationToken);
    }

    private static IEnumerable<int> GetStatusIds(EventRecord record)
    {
        var payload = record.Payload.RootElement;
        var field = GetPayloadValue(payload, "field");
        var isStatusChange = string.Equals(field, "status", StringComparison.OrdinalIgnoreCase);

        if (!isStatusChange)
        {
            yield break;
        }

        var oldValueIsStatusId = int.TryParse(GetPayloadValue(payload, "oldValue"), out var oldStatusId);

        if (oldValueIsStatusId)
        {
            yield return oldStatusId;
        }

        var newValueIsStatusId = int.TryParse(GetPayloadValue(payload, "newValue"), out var newStatusId);

        if (newValueIsStatusId)
        {
            yield return newStatusId;
        }
    }

    private static string BuildSummary(
        EventRecord record,
        ActivityType activityType,
        IReadOnlyDictionary<int, string> statusNames)
    {
        var payload = record.Payload.RootElement;
        var summary = record.EventKey switch
        {
            EventKeys.EntityCreated => FormatCreatedSummary(payload),
            EventKeys.EntityDeleted => "Entity deleted",
            EventKeys.EntityRestored => "Entity restored",
            EventKeys.EntityFieldTransitioned => FormatFieldTransition(payload, statusNames),
            EventKeys.ScopeMemberChanged => FormatMemberChange(payload),
            EventKeys.ScopeMemberAttributeChanged => FormatMemberAttributeChange(payload),
            EventKeys.ScopeLifecycleTransitioned => FormatLifecycleChange(payload),
            EventKeys.SecurityLoginSucceeded => FormatAuthentication(payload, succeeded: true),
            EventKeys.SecurityLoginFailed => FormatAuthentication(payload, succeeded: false),
            EventKeys.WorkspaceSettingsChanged => FormatWorkspaceSettingsChange(payload),
            _ => FormatLegacySummary(payload, activityType, statusNames),
        };

        return Truncate(summary, 140);
    }

    private static string FormatCreatedSummary(JsonElement payload)
    {
        var name = GetPayloadValue(payload, "name");

        return name is null ? "Entity created" : $"Created {FormatValue(name)}";
    }

    private static string FormatFieldTransition(JsonElement payload, IReadOnlyDictionary<int, string> statusNames)
    {
        var fieldValue = GetPayloadValue(payload, "field") ?? "field";
        var field = Humanize(fieldValue);
        var oldValue = ResolveFieldValue(fieldValue, GetTransitionValue(payload, "old"), statusNames);
        var newValue = ResolveFieldValue(fieldValue, GetTransitionValue(payload, "new"), statusNames);

        return $"{field}: {FormatValue(oldValue)} → {FormatValue(newValue)}";
    }

    private static string FormatMemberChange(JsonElement payload)
    {
        var memberType = Humanize(GetPayloadValue(payload, "memberType") ?? "member");
        var memberId = GetPayloadValue(payload, "memberId");
        var change = Humanize(GetPayloadValue(payload, "change") ?? "changed").ToLowerInvariant();
        var member = memberId is null ? memberType : $"{memberType} {FormatValue(memberId)}";

        return $"{member} {change}";
    }

    private static string FormatMemberAttributeChange(JsonElement payload)
    {
        var memberType = Humanize(GetPayloadValue(payload, "memberType") ?? "member");
        var memberId = FormatValue(GetPayloadValue(payload, "memberId"));
        var field = Humanize(GetPayloadValue(payload, "field") ?? "field");
        var oldValue = GetTransitionValue(payload, "old");
        var newValue = GetTransitionValue(payload, "new");

        return $"{memberType} {memberId} {field}: {FormatValue(oldValue)} → {FormatValue(newValue)}";
    }

    private static string FormatLifecycleChange(JsonElement payload)
    {
        var state = GetPayloadValue(payload, "state");

        return state is null ? "Lifecycle changed" : $"State: {Humanize(state)}";
    }

    private static string FormatAuthentication(JsonElement payload, bool succeeded)
    {
        var outcome = succeeded ? "Login succeeded" : "Login failed";
        var email = GetPayloadValue(payload, "email");
        var method = GetPayloadValue(payload, "method");
        var subject = email is null ? outcome : $"{outcome} for {FormatValue(email)}";

        return method is null ? subject : $"{subject} via {Humanize(method).ToLowerInvariant()}";
    }

    private static string FormatWorkspaceSettingsChange(JsonElement payload)
    {
        var hasFields = payload.TryGetProperty("fields", out var fields) && fields.ValueKind is JsonValueKind.Array;

        if (!hasFields)
        {
            return "Workspace settings changed";
        }

        var labels = fields
            .EnumerateArray()
            .Where(field => field.ValueKind is JsonValueKind.String)
            .Select(field => field.GetString())
            .Where(field => field is not null)
            .Cast<string>()
            .Select(Humanize)
            .ToList();

        return labels.Count == 0
            ? "Workspace settings changed"
            : $"Changed {string.Join(", ", labels)}";
    }

    private static string FormatLegacySummary(
        JsonElement payload,
        ActivityType activityType,
        IReadOnlyDictionary<int, string> statusNames)
    {
        var oldValue = GetPayloadValue(payload, "oldValue");
        var newValue = GetPayloadValue(payload, "newValue");
        var hasTransition = oldValue is not null || newValue is not null;

        if (hasTransition)
        {
            var fieldValue = GetPayloadValue(payload, "field") ?? activityType.ToString();
            var field = Humanize(fieldValue);
            oldValue = ResolveFieldValue(fieldValue, oldValue, statusNames);
            newValue = ResolveFieldValue(fieldValue, newValue, statusNames);

            return $"{field}: {FormatValue(oldValue)} → {FormatValue(newValue)}";
        }

        var tagName = GetPayloadValue(payload, "tagName");

        if (tagName is not null)
        {
            return $"Tag: {FormatValue(tagName)}";
        }

        var relatedTask = GetPayloadValue(payload, "relatedTaskSystemId");

        if (relatedTask is not null)
        {
            var relation = GetPayloadValue(payload, "label") ?? GetPayloadValue(payload, "relationTypeName");

            return $"Relation: {FormatValue(relation)} {FormatValue(relatedTask)}";
        }

        var group = GetPayloadValue(payload, "group");

        if (group is not null)
        {
            return $"Group: {FormatValue(group)}";
        }

        var permission = GetPayloadValue(payload, "permission");

        if (permission is not null)
        {
            var granted = GetPayloadValue(payload, "granted") == bool.TrueString;

            return $"Permission: {FormatValue(permission)} {(granted ? "granted" : "revoked")}";
        }

        var oldRole = GetPayloadValue(payload, "oldRole");
        var newRole = GetPayloadValue(payload, "newRole");
        var hasRoleChange = oldRole is not null || newRole is not null;

        if (hasRoleChange)
        {
            return $"Role: {FormatValue(oldRole)} → {FormatValue(newRole)}";
        }

        var fileName = GetPayloadValue(payload, "fileName");

        if (fileName is not null)
        {
            return $"File: {FormatValue(fileName)}";
        }

        var assigneeId = GetPayloadValue(payload, "assigneeId");

        if (assigneeId is not null)
        {
            return $"Assignee: {FormatValue(assigneeId)}";
        }

        var exportType = GetPayloadValue(payload, "exportType");

        if (exportType is not null)
        {
            return $"Export: {Humanize(exportType)}";
        }

        var hasEmails = payload.TryGetProperty("emails", out var emails) && emails.ValueKind is JsonValueKind.Array;

        if (hasEmails)
        {
            var values = emails
                .EnumerateArray()
                .Where(email => email.ValueKind is JsonValueKind.String)
                .Select(email => email.GetString())
                .Where(email => email is not null)
                .Cast<string>()
                .ToList();
            var visibleEmails = string.Join(", ", values.Take(2));
            var remainingCount = values.Count - 2;
            var suffix = remainingCount > 0 ? $" +{remainingCount} more" : string.Empty;

            return values.Count == 0 ? "Users changed" : $"Users: {visibleEmails}{suffix}";
        }

        return "No additional details";
    }

    private static string? ResolveFieldValue(
        string field,
        string? value,
        IReadOnlyDictionary<int, string> statusNames)
    {
        var isStatus = string.Equals(field, "status", StringComparison.OrdinalIgnoreCase);
        var valueIsStatusId = int.TryParse(value, out var statusId);
        var canResolveStatus = isStatus && valueIsStatusId;

        if (!canResolveStatus)
        {
            return value;
        }

        var hasStatusName = statusNames.TryGetValue(statusId, out var statusName);

        return hasStatusName ? statusName : value;
    }

    private static string? GetTransitionValue(JsonElement payload, string prefix)
    {
        return GetPayloadValue(payload, $"{prefix}Value")
            ?? GetPayloadValue(payload, $"{prefix}Category")
            ?? GetPayloadValue(payload, $"{prefix}NumericValue");
    }

    private static string? GetPayloadValue(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static string FormatValue(string? value)
    {
        return value is null ? "None" : Truncate(value, 48);
    }

    private static string Humanize(string value)
    {
        var normalized = value
            .Replace("startdate", "start date", StringComparison.OrdinalIgnoreCase)
            .Replace("duedate", "due date", StringComparison.OrdinalIgnoreCase);
        var words = new List<char>(normalized.Length + 4);

        foreach (var character in normalized)
        {
            var needsSeparator = char.IsUpper(character) && words.Count > 0 && words[^1] != ' ';

            if (needsSeparator)
            {
                words.Add(' ');
            }

            words.Add(character);
        }

        var result = new string([.. words]).Trim();

        return result.Length == 0
            ? result
            : char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }

    private static string? GetPayloadString(JsonElement payload, string propertyName)
    {
        return payload.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

}
