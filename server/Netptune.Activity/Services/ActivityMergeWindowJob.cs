using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;

namespace Netptune.Activity.Services;

// Closes expired merge windows and fans out the notifications deferred while they were open. ActivityHandler
// raises no notifications at all for mergeable field edits, so this is the only thing that ever notifies for
// them.
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

        var slugsByWorkspace = await unitOfWork.Workspaces.GetSlugsByIds(workspaceIds, cancellationToken);
        var usersByWorkspace = await unitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds, cancellationToken);

        var ancestorsByEntity = new Dictionary<(Core.Enums.EntityType, int), ActivityAncestors>();

        foreach (var entry in entries)
        {

            if (!slugsByWorkspace.TryGetValue(entry.WorkspaceId, out var slug))
            {
                continue;
            }

            if (!usersByWorkspace.TryGetValue(entry.WorkspaceId, out var allUserIds))
            {
                continue;
            }

            if (entry.LastEventRecordId == 0)
            {
                Logger.LogWarning("ActivityMergeWindowJob: entry {EntryId} has no source ledger row, skipping", entry.Id);

                continue;
            }

            var recipients = allUserIds.Where(userId => userId != entry.UserId).ToList();

            if (recipients.Count == 0)
            {
                continue;
            }

            if (!ancestorsByEntity.TryGetValue((entry.EntityType, entry.EntityId), out var ancestors))
            {
                ancestors = await ActivityLinks.Resolve(
                    unitOfWork,
                    entry.EntityType,
                    entry.EntityId,
                    cancellationToken);
                ancestorsByEntity[(entry.EntityType, entry.EntityId)] = ancestors;
            }

            var link = ActivityLinks.Build(
                slug,
                entry.EntityType,
                entry.EntityId,
                ancestors);

            notifications.AddRange(recipients.Select(userId => new Notification
            {
                UserId = userId,
                EventRecordId = entry.LastEventRecordId,
                ActivityEntryId = entry.Id,
                IsRead = false,
                Link = link,
                WorkspaceId = entry.WorkspaceId,
                EntityType = entry.EntityType,
                ActivityType = entry.ActivityType,
                CreatedByUserId = entry.UserId,
                OwnerId = entry.UserId,
            }));
        }

        return notifications;
    }
}
