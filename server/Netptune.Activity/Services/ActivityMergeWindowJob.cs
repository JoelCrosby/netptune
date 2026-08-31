using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

namespace Netptune.Activity.Services;

public sealed class ActivityMergeWindowJob : BackgroundService
{
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ActivityMergeOptions Merge;
    private readonly ILogger<ActivityMergeWindowJob> Logger;

    public ActivityMergeWindowJob(
        IServiceScopeFactory scopeFactory,
        IOptions<ActivityMergeOptions> merge,
        ILogger<ActivityMergeWindowJob> logger)
    {
        ScopeFactory = scopeFactory;
        Merge = merge.Value;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "ActivityMergeWindowJob failed");
            }

            await Task.Delay(Merge.SweepInterval, stoppingToken);
        }
    }

    internal async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var sweptCount = await SweepAsync(
            scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>(),
            scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>(),
            cancellationToken);

        return sweptCount;
    }

    internal async Task<int> SweepAsync(
        INetptuneUnitOfWork unitOfWork,
        INotificationEventPublisher notificationEvents,
        CancellationToken cancellationToken)
    {
        var notified = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var claimed = await unitOfWork.ActivityEntries.ClaimExpiredEntries(BatchSize, cancellationToken);

            if (claimed.Count == 0)
            {
                break;
            }

            notified += await FinaliseAsync(
                unitOfWork,
                notificationEvents,
                claimed,
                cancellationToken);

            if (claimed.Count < BatchSize)
            {
                break;
            }
        }

        return notified;
    }

    private async Task<int> FinaliseAsync(
        INetptuneUnitOfWork unitOfWork,
        INotificationEventPublisher notificationEvents,
        List<ActivityEntry> claimed,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var live = new List<ActivityEntry>();

        foreach (var entry in claimed)
        {

            if (!ActivityEntryMeta.IsNoOpBurst(entry.Meta))
            {
                live.Add(entry);

                continue;
            }

            entry.IsDeleted = true;
            entry.IsOpen = false;
            entry.UpdatedAt = now;

            Logger.LogDebug(
                "ActivityMergeWindowJob: discarded no-op burst {EntryId} ({RevisionCount} revisions)",
                entry.Id,
                entry.RevisionCount);
        }

        var notifications = await BuildNotificationsAsync(unitOfWork, live, cancellationToken);

        if (notifications.Count > 0)
        {
            await unitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return 0;
        }

        await notificationEvents.PublishManyAsync(
            notifications.Select(notification => new UserNotificationEvent(
                notification.UserId,
                new NotificationEvent(notification.Id, false))),
            cancellationToken);

        return notifications.Count;
    }

    private async Task<List<Notification>> BuildNotificationsAsync(
        INetptuneUnitOfWork unitOfWork,
        List<ActivityEntry> entries,
        CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();

        if (entries.Count == 0)
        {
            return notifications;
        }

        var workspaceIds = entries.Select(entry => entry.WorkspaceId).Distinct().ToList();

        var usersByWorkspace = await unitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds, cancellationToken);

        var matchRequests = entries.Select(ToMatchRequest).ToList();
        var fanOut = await NotificationSubscriptionFanOut.Build(unitOfWork, matchRequests, cancellationToken);

        foreach (var entry in entries)
        {

            if (!usersByWorkspace.TryGetValue(entry.WorkspaceId, out var allUserIds))
            {
                continue;
            }

            if (entry.LastEventRecordId == 0)
            {
                Logger.LogWarning("ActivityMergeWindowJob: entry {EntryId} has no source ledger row, skipping", entry.Id);

                continue;
            }

            var addressedUserIds = ReadRecipientUserIds(entry.Meta);
            var subscribedUserIds = fanOut.Recipients(ToMatchRequest(entry));
            var requestedUserIds = addressedUserIds.Concat(subscribedUserIds).Distinct().ToList();
            var recipients = await NotificationRecipientResolver.Resolve(
                unitOfWork,
                new NotificationRecipientRequest
                {
                    RequestedUserIds = requestedUserIds,
                    WorkspaceUserIds = allUserIds,
                    ActorUserId = entry.UserId,
                    WorkspaceId = entry.WorkspaceId,
                    ActivityType = entry.ActivityType,
                },
                cancellationToken);

            if (recipients.Count == 0)
            {
                continue;
            }

            notifications.AddRange(recipients.Select(userId => new Notification
            {
                UserId = userId,
                EventRecordId = entry.LastEventRecordId,
                ActivityEntryId = entry.Id,
                IsRead = false,
                WorkspaceId = entry.WorkspaceId,
                EntityType = entry.EntityType,
                ActivityType = entry.ActivityType,
                CreatedByUserId = entry.UserId,
                OwnerId = entry.UserId,
            }));
        }

        return notifications;
    }

    // No payload: a merged entry stands for a burst of field edits, which is a change within the
    // scopes the task already sits in rather than a move between them.
    private static NotificationSubscriptionMatchRequest ToMatchRequest(ActivityEntry entry)
    {
        return new NotificationSubscriptionMatchRequest
        {
            WorkspaceId = entry.WorkspaceId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            ActivityType = entry.ActivityType,
        };
    }

    private static List<string> ReadRecipientUserIds(JsonDocument? meta)
    {
        if (meta is null)
        {
            return [];
        }

        var hasRecipients = meta.RootElement.TryGetProperty("recipientUserIds", out var recipients);
        var recipientsAreArray = hasRecipients && recipients.ValueKind == JsonValueKind.Array;

        if (!recipientsAreArray)
        {
            return [];
        }

        return recipients
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Cast<string>()
            .ToList();
    }
}
