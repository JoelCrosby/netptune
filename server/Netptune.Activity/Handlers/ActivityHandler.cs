using System.Text.Json;

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

    private sealed record ActivityRecord(ActivityEvent Event, ActivityLog Log, ActivityAncestors Ancestors)
    {
        public int WorkspaceId => Log.WorkspaceId;

        public EntityType EntityType => Log.EntityType;

        public int EntityId => Log.EntityId!.Value;

        public string UserId => Log.UserId;
    }

    public async ValueTask<Unit> Handle(ActivityMessage request, CancellationToken cancellationToken)
    {
        var events = await FilterProcessedEvents(request.Events, cancellationToken);

        if (events.Count == 0) return default;

        var records = new List<ActivityRecord>();

        foreach (var activity in events)
        {
            if (activity.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = await ActivityLinks.Resolve(UnitOfWork, activity.EntityType, activity.EntityId.Value, cancellationToken);

            var log = new ActivityLog
            {
                EventId = activity.EventId,
                OwnerId = activity.UserId,
                Type = activity.Type,
                EntityType = activity.EntityType,
                EntityId = activity.EntityId,
                UserId = activity.UserId,
                CreatedByUserId = activity.UserId,
                WorkspaceId = activity.WorkspaceId,
                TaskId = ancestors.TaskId,
                ProjectId = ancestors.ProjectId,
                BoardId = ancestors.BoardId,
                BoardGroupId = ancestors.BoardGroupId,
                OccurredAt = activity.OccurredAt,
                Meta = BuildMeta(activity),
                BoardSlug = ancestors.BoardKey,
                ProjectSlug = ancestors.ProjectKey,
                WorkspaceSlug = ancestors.WorkspaceKey,
            };

            await UnitOfWork.ActivityLogs.AddAsync(log, cancellationToken);

            records.Add(new (activity, log, ancestors));
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

    private bool IsDiscrete(ActivityRecord record) => !Merge.IsMergeable(record.Log.Type);

    private async Task<List<ActivityEvent>> FilterProcessedEvents(List<ActivityEvent> events, CancellationToken cancellationToken)
    {
        var eventIds = events.Select(activity => activity.EventId).ToList();

        if (eventIds.Count == 0) return events;

        var processed = await UnitOfWork.ActivityLogs.GetExistingEventIds(eventIds, cancellationToken);
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
                workspaceId, entityType, entityId, userId, now, cancellationToken);

            var mergeable = group.Where(record => !IsDiscrete(record)).ToList();

            foreach (var record in group.Where(IsDiscrete))
            {
                await UnitOfWork.ActivityEntries.AddAsync(BuildDiscreteEntry(record, now), cancellationToken);
            }

            if (mergeable.Count == 0) continue;

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
                upsert, now, Merge.WindowDuration, Merge.MaxWindowDuration, cancellationToken);

            if (result is UpsertEntryResult.Upserted) return;

            await UnitOfWork.ActivityEntries.CloseStaleEntry(
                upsert.WorkspaceId, upsert.EntityType, upsert.EntityId, upsert.UserId, now, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Could not upsert an activity entry for entity {upsert.EntityType}:{upsert.EntityId} after {MaxUpsertAttempts} attempts.");
    }

    private static ActivityEntryUpsert BuildUpsert(List<ActivityRecord> mergeable)
    {
        var events = mergeable.Select(record => record.Event).ToList();
        var last = mergeable[^1];

        var types = mergeable.Select(record => record.Log.Type).Distinct().ToList();

        return new ()
        {
            WorkspaceId = last.WorkspaceId,
            EntityType = last.EntityType,
            EntityId = last.EntityId,
            UserId = last.UserId,

            ActivityType = types.Count == 1 ? types[0] : ActivityType.Modify,

            ChangedFields = ActivityEntryMeta.ChangedFields(events),
            MetaJson = ActivityEntryMeta.Build(events),
            LastActivityLogId = last.Log.Id,

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

        return new ()
        {
            WorkspaceId = record.WorkspaceId,
            WorkspaceSlug = record.Ancestors.WorkspaceKey,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            UserId = record.UserId,
            ActivityType = record.Log.Type,
            ChangedFields = ActivityEntryMeta.ChangedFields(events),
            Meta = record.Log.Meta is null ? null : JsonDocument.Parse(record.Log.Meta.RootElement.GetRawText()),
            LastActivityLogId = record.Log.Id,
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
        if (records.Count == 0) return [];

        var workspaceIds = records.Select(record => record.WorkspaceId).Distinct().ToList();

        var slugsByWorkspace = await UnitOfWork.Workspaces.GetSlugsByIds(workspaceIds, cancellationToken);
        var usersByWorkspace = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds, cancellationToken);

        var allNotifications = new List<Notification>();

        foreach (var (activity, log, ancestors) in records)
        {
            if (AuditOnlyTypes.Contains(log.Type)) continue;

            if (!slugsByWorkspace.TryGetValue(log.WorkspaceId, out var slug)) continue;
            if (!usersByWorkspace.TryGetValue(log.WorkspaceId, out var allUserIds)) continue;

            var recipientUserIds = activity.RecipientUserIds;

            var recipients = recipientUserIds is { Count: > 0 }
                ? recipientUserIds.Where(id => id != log.UserId && allUserIds.Contains(id)).ToList()
                : allUserIds.Where(id => id != log.UserId).ToList();

            if (recipients.Count == 0) continue;

            var link = ActivityLinks.Build(slug, log.EntityType, log.EntityId, ancestors);

            allNotifications.AddRange(recipients.Select(userId => new Notification
            {
                UserId = userId,
                ActivityLogId = log.Id,
                IsRead = false,
                Link = link,
                WorkspaceId = log.WorkspaceId,
                EntityType = log.EntityType,
                ActivityType = log.Type,
                CreatedByUserId = log.UserId,
                OwnerId = log.UserId,
            }));
        }

        if (allNotifications.Count == 0) return allNotifications;

        await UnitOfWork.Notifications.AddRangeAsync(allNotifications, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return allNotifications;
    }

    private static JsonDocument? BuildMeta(ActivityEvent activity)
    {
        if (activity.IpAddress is null && activity.UserAgent is null && activity.Meta is null)
        {
            return null;
        }

        var dict = new Dictionary<string, object?>();

        if (activity.Meta is not null)
        {
            var existing = JsonDocument.Parse(activity.Meta);
            foreach (var prop in existing.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
        }

        if (activity.IpAddress is not null) dict["ipAddress"] = activity.IpAddress;
        if (activity.UserAgent is not null) dict["userAgent"] = activity.UserAgent;

        return JsonDocument.Parse(JsonSerializer.Serialize(dict));
    }

    private Task PublishNotificationEventsAsync(
        List<Notification> notifications,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0) return Task.CompletedTask;

        var events = notifications.Select(notification =>
            new UserNotificationEvent(
                notification.UserId,
                new NotificationEvent(notification.Id, false)));

        return NotificationEvents.PublishManyAsync(events, cancellationToken);
    }

    #endregion
}
