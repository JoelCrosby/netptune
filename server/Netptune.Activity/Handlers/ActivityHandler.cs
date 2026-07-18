using System.Text.Json;
using System.Net;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Activity.Services;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;

namespace Netptune.Activity.Handlers;

public sealed class ActivityHandler : IRequestHandler<ActivityMessage>
{
    private static readonly HashSet<ActivityType> AuditOnlyTypes =
    [
        ActivityType.ExportRequested,
        ActivityType.LoginSuccess,
        ActivityType.LoginFailed,
    ];

    private const int MaxUpsertAttempts = 3;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly INotificationEventPublisher NotificationEvents;
    private readonly ActivityMergeOptions Merge;

    public ActivityHandler(
        INetptuneUnitOfWork unitOfWork,
        INotificationEventPublisher notificationEvents,
        IOptions<ActivityMergeOptions> merge)
    {
        UnitOfWork = unitOfWork;
        NotificationEvents = notificationEvents;
        Merge = merge.Value;
    }

    private sealed record ActivityRecord(ActivityEvent Event, EventRecord Log, ActivityAncestors Ancestors)
    {
        public int WorkspaceId => Event.WorkspaceId;

        public EntityType EntityType => Event.EntityType;

        public int EntityId => Event.EntityId!.Value;

        public string UserId => Event.UserId;
    }

    public async ValueTask<Unit> Handle(ActivityMessage request, CancellationToken cancellationToken)
    {
        var events = await FilterProcessedEvents(request.Events, cancellationToken);

        if (events.Count == 0)
        {
            return default;
        }

        var records = new List<ActivityRecord>();

        foreach (var activity in events)
        {

            if (activity.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = await ActivityLinks.Resolve(
                UnitOfWork,
                activity.EntityType,
                activity.EntityId.Value,
                cancellationToken);

            var log = new EventRecord
            {
                EventId = activity.EventId,
                WorkspaceId = activity.WorkspaceId,
                EventKey = EventKeys.EntityActivityRecorded,
                SchemaVersion = 1,
                SubjectType = EventEntityTypes.From(activity.EntityType),
                SubjectId = activity.EntityId.Value.ToString(),
                OccurredAt = activity.OccurredAt,
                RecordedAt = DateTime.UtcNow,
                ActorUserId = activity.UserId,
                IpAddress = IPAddress.TryParse(activity.IpAddress, out var ipAddress) ? ipAddress : null,
                UserAgent = activity.UserAgent,
                RetentionClass = EventRetentionClasses.Audit,
                Payload = BuildPayload(activity, ancestors),
                References = BuildReferences(activity, ancestors),
            };

            await UnitOfWork.EventRecords.AddAsync(log, cancellationToken);

            records.Add(new(activity, log, ancestors));
        }

        var notifications = new List<Notification>();

        // One transaction, because a committed ledger row is what FilterProcessedEvents treats as "already
        // processed". If the ledger could commit without the entries and notifications it stands for, a pod
        // killed mid-message (KEDA scales these down routinely) would leave the events marked processed with
        // nothing to show for them, and the redelivery would filter them out for good.
        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.CompleteAsync(cancellationToken);

            await ProjectEntriesAsync(records, cancellationToken);

            notifications = await CreateNotificationsAsync(records.Where(IsDiscrete).ToList(), cancellationToken);
        });

        // After the commit only — the push carries the notification's id, and a client that fetches it before
        // the transaction lands is told about a notification that does not exist yet.
        await PublishNotificationEventsAsync(notifications, cancellationToken);

        return default;
    }

    private bool IsDiscrete(ActivityRecord record) => !Merge.IsMergeable(record.Event.Type);

    private async Task<List<ActivityEvent>> FilterProcessedEvents(List<ActivityEvent> events, CancellationToken cancellationToken)
    {
        var eventIds = events.Select(activity => activity.EventId).ToList();

        if (eventIds.Count == 0)
        {
            return events;
        }

        var processed = await UnitOfWork.EventRecords.GetExistingEventIds(eventIds, cancellationToken);
        var seen = new HashSet<Guid>();

        return events
            .Where(activity => !processed.Contains(activity.EventId) && seen.Add(activity.EventId))
            .ToList();
    }

    #region Entries

    private async Task ProjectEntriesAsync(List<ActivityRecord> records, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var groups = records.GroupBy(record => (record.WorkspaceId, record.EntityType, record.EntityId, record.UserId));

        foreach (var group in groups)
        {
            var (workspaceId, entityType, entityId, userId) = group.Key;

            await UnitOfWork.ActivityEntries.ExpireEntriesForOtherUsers(
                workspaceId,
                entityType,
                entityId,
                userId,
                now,
                cancellationToken);

            var mergeable = group.Where(record => !IsDiscrete(record)).ToList();

            foreach (var record in group.Where(IsDiscrete))
            {
                await UnitOfWork.ActivityEntries.AddAsync(BuildDiscreteEntry(record, now), cancellationToken);
            }

            if (mergeable.Count == 0)
            {
                continue;
            }

            await UpsertEntryAsync(mergeable, now, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task UpsertEntryAsync(List<ActivityRecord> mergeable, DateTime now, CancellationToken cancellationToken)
    {
        var upsert = BuildUpsert(mergeable);

        for (var attempt = 0; attempt < MaxUpsertAttempts; attempt++)
        {
            var result = await UnitOfWork.ActivityEntries.UpsertEntry(
                upsert,
                now,
                Merge.WindowDuration,
                Merge.MaxWindowDuration,
                cancellationToken);

            if (result is UpsertEntryResult.Upserted)
            {
                return;
            }

            await UnitOfWork.ActivityEntries.CloseStaleEntry(
                upsert.WorkspaceId,
                upsert.EntityType,
                upsert.EntityId,
                upsert.UserId,
                now,
                cancellationToken);
        }

        throw new InvalidOperationException(
            $"Could not upsert an activity entry for entity {upsert.EntityType}:{upsert.EntityId} after {MaxUpsertAttempts} attempts.");
    }

    private static ActivityEntryUpsert BuildUpsert(List<ActivityRecord> mergeable)
    {
        var events = mergeable.Select(record => record.Event).ToList();
        var last = mergeable[^1];

        var types = mergeable.Select(record => record.Event.Type).Distinct().ToList();

        return new()
        {
            WorkspaceId = last.WorkspaceId,
            EntityType = last.EntityType,
            EntityId = last.EntityId,
            UserId = last.UserId,

            ActivityType = types.Count == 1 ? types[0] : ActivityType.Modify,

            ChangedFields = ActivityEntryMeta.ChangedFields(events),
            MetaJson = ActivityEntryMeta.Build(events),
            LastEventRecordId = last.Log.Id,

            FirstOccurredAt = mergeable.Min(record => record.Log.OccurredAt),
            LastOccurredAt = mergeable.Max(record => record.Log.OccurredAt),
            RevisionCount = mergeable.Count,

            WorkspaceSlug = last.Ancestors.WorkspaceKey,
            ProjectId = last.Ancestors.ProjectId,
            ProjectSlug = last.Ancestors.ProjectKey,
            BoardId = last.Ancestors.BoardId,
            BoardSlug = last.Ancestors.BoardKey,
            BoardGroupId = last.Ancestors.BoardGroupId,
            TaskId = last.Ancestors.TaskId,
        };
    }

    private static ActivityEntry BuildDiscreteEntry(ActivityRecord record, DateTime now)
    {
        var events = new List<ActivityEvent> { record.Event };

        return new()
        {
            WorkspaceId = record.WorkspaceId,
            WorkspaceSlug = record.Ancestors.WorkspaceKey,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            UserId = record.UserId,
            ActivityType = record.Event.Type,
            ChangedFields = ActivityEntryMeta.ChangedFields(events),
            Meta = JsonDocument.Parse(record.Log.Payload.RootElement.GetRawText()),
            LastEventRecordId = record.Log.Id,
            FirstOccurredAt = record.Log.OccurredAt,
            LastOccurredAt = record.Log.OccurredAt,
            RevisionCount = 1,
            IsOpen = false,
            WindowExpiresAt = record.Log.OccurredAt,
            NotifiedAt = now,
            ProjectId = record.Ancestors.ProjectId,
            ProjectSlug = record.Ancestors.ProjectKey,
            BoardId = record.Ancestors.BoardId,
            BoardSlug = record.Ancestors.BoardKey,
            BoardGroupId = record.Ancestors.BoardGroupId,
            TaskId = record.Ancestors.TaskId,
            CreatedByUserId = record.UserId,
            OwnerId = record.UserId,
        };
    }

    #endregion

    #region Notifications

    private async Task<List<Notification>> CreateNotificationsAsync(List<ActivityRecord> records, CancellationToken cancellationToken)
    {

        if (records.Count == 0)
        {
            return [];
        }

        var workspaceIds = records.Select(record => record.WorkspaceId).Distinct().ToList();

        var slugsByWorkspace = await UnitOfWork.Workspaces.GetSlugsByIds(workspaceIds, cancellationToken);
        var usersByWorkspace = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds, cancellationToken);

        var allNotifications = new List<Notification>();

        foreach (var (activity, log, ancestors) in records)
        {

            if (AuditOnlyTypes.Contains(activity.Type))
            {
                continue;
            }

            if (!slugsByWorkspace.TryGetValue(activity.WorkspaceId, out var slug))
            {
                continue;
            }

            if (!usersByWorkspace.TryGetValue(activity.WorkspaceId, out var allUserIds))
            {
                continue;
            }

            var recipientUserIds = activity.RecipientUserIds;

            var recipients = recipientUserIds is { Count: > 0 }
                ? recipientUserIds.Where(id => id != activity.UserId && allUserIds.Contains(id)).ToList()
                : allUserIds.Where(id => id != activity.UserId).ToList();

            if (recipients.Count == 0)
            {
                continue;
            }

            var link = ActivityLinks.Build(
                slug,
                activity.EntityType,
                activity.EntityId,
                ancestors);

            allNotifications.AddRange(recipients.Select(userId => new Notification
            {
                UserId = userId,
                EventRecordId = log.Id,
                IsRead = false,
                Link = link,
                WorkspaceId = activity.WorkspaceId,
                EntityType = activity.EntityType,
                ActivityType = activity.Type,
                CreatedByUserId = activity.UserId,
                OwnerId = activity.UserId,
            }));
        }

        if (allNotifications.Count == 0)
        {
            return allNotifications;
        }

        await UnitOfWork.Notifications.AddRangeAsync(allNotifications, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return allNotifications;
    }

    private static JsonDocument BuildPayload(ActivityEvent activity, ActivityAncestors ancestors)
    {
        var dict = new Dictionary<string, object?>
        {
            ["activityType"] = (int)activity.Type,
            ["field"] = activity.Field?.ToString(),
            ["oldValue"] = activity.OldValue,
            ["newValue"] = activity.NewValue,
            ["oldValueHash"] = activity.OldValueHash,
            ["newValueHash"] = activity.NewValueHash,
            ["recipientUserIds"] = activity.RecipientUserIds,
            ["workspaceSlug"] = ancestors.WorkspaceKey,
            ["projectSlug"] = ancestors.ProjectKey,
            ["boardSlug"] = ancestors.BoardKey,
        };

        if (activity.Meta is not null)
        {
            var existing = JsonDocument.Parse(activity.Meta);
            foreach (var prop in existing.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(dict));
    }

    private static HashSet<EventReference> BuildReferences(ActivityEvent activity, ActivityAncestors ancestors)
    {
        var references = new HashSet<EventReference>();

        AddReference(
            references,
            EventReferenceRoles.Scope,
            EntityType.Project,
            ancestors.ProjectId);
        AddReference(
            references,
            EventReferenceRoles.Scope,
            EntityType.Board,
            ancestors.BoardId);
        AddReference(
            references,
            EventReferenceRoles.Scope,
            EntityType.BoardGroup,
            ancestors.BoardGroupId);
        AddReference(
            references,
            EventReferenceRoles.Parent,
            EntityType.Task,
            ancestors.TaskId);

        return references;
    }

    private static void AddReference(
        ISet<EventReference> references,
        string role,
        EntityType entityType,
        int? entityId)
    {

        if (!entityId.HasValue)
        {
            return;
        }

        references.Add(new EventReference
        {
            Role = role,
            EntityType = EventEntityTypes.From(entityType),
            EntityId = entityId.Value.ToString(),
        });
    }

    private Task PublishNotificationEventsAsync(
        List<Notification> notifications,
        CancellationToken cancellationToken)
    {

        if (notifications.Count == 0)
        {
            return Task.CompletedTask;
        }

        var events = notifications.Select(notification =>
            new UserNotificationEvent(
                notification.UserId,
                new NotificationEvent(notification.Id, false)));

        return NotificationEvents.PublishManyAsync(events, cancellationToken);
    }

    #endregion
}
