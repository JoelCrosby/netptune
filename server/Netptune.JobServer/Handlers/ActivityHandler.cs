using System.Text.Json;
using System.Text.Json.Serialization;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

using StackExchange.Redis;

namespace Netptune.JobServer.Handlers;

[JsonSerializable(typeof(JobNotificationEvent))]
internal partial class ActivityHandlerSerializerContext : JsonSerializerContext;

public record JobNotificationEvent(int NotificationId, bool IsRead);

public sealed class ActivityHandler : IRequestHandler<ActivityMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IAncestorService AncestorService;
    private readonly IConnectionMultiplexer Redis;

    public ActivityHandler(INetptuneUnitOfWork unitOfWork, IAncestorService ancestorService, IConnectionMultiplexer redis)
    {
        UnitOfWork = unitOfWork;
        AncestorService = ancestorService;
        Redis = redis;
    }

    public async ValueTask<Unit> Handle(ActivityMessage request, CancellationToken cancellationToken)
    {
        var activityLogs = new List<ActivityLog>();

        foreach (var activity in request.Events)
        {
            if (activity.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = await AncestorService.GetTaskAncestors(activity.EntityId.Value);

            var log = new ActivityLog
            {
                OwnerId = activity.UserId,
                Type = activity.Type,
                EntityType = activity.EntityType,
                EntityId = activity.EntityId,
                UserId = activity.UserId,
                CreatedByUserId = activity.UserId,
                WorkspaceId = activity.WorkspaceId,
                TaskId = activity.EntityId,
                ProjectId = ancestors.ProjectId,
                BoardId = ancestors.ProjectId,
                BoardGroupId = ancestors.BoardGroupId,
                Time = activity.Time,
                Meta = activity.Meta is not null ? JsonDocument.Parse(activity.Meta) : null,
            };

            await UnitOfWork.ActivityLogs.AddAsync(log);
            activityLogs.Add(log);
        }

        await UnitOfWork.CompleteAsync();

        await CreateNotificationsAsync(activityLogs);

        return default;
    }

    private async Task CreateNotificationsAsync(List<ActivityLog> activityLogs)
    {
        var workspaceIds = activityLogs.Select(l => l.WorkspaceId).Distinct().ToList();

        var slugsByWorkspace = await UnitOfWork.Workspaces.GetSlugsByIds(workspaceIds);
        var usersByWorkspace = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds);

        var allNotifications = new List<Notification>();

        foreach (var log in activityLogs)
        {
            if (!slugsByWorkspace.TryGetValue(log.WorkspaceId, out var slug)) continue;
            if (!usersByWorkspace.TryGetValue(log.WorkspaceId, out var allUserIds)) continue;

            var recipients = allUserIds.Where(id => id != log.UserId).ToList();
            if (recipients.Count == 0) continue;

            var link = BuildLink(slug, log);

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

        if (allNotifications.Count == 0) return;

        await UnitOfWork.Notifications.AddRangeAsync(allNotifications);
        await UnitOfWork.CompleteAsync();

        await PublishNotificationEventsAsync(allNotifications);
    }

    private static string BuildLink(string workspaceSlug, ActivityLog log)
    {
        return log.EntityType switch
        {
            Core.Enums.EntityType.Task => $"/{workspaceSlug}/tasks",
            Core.Enums.EntityType.Board => $"/{workspaceSlug}/boards",
            Core.Enums.EntityType.Project => $"/{workspaceSlug}/projects",
            Core.Enums.EntityType.Workspace => $"/{workspaceSlug}",
            _ => $"/{workspaceSlug}",
        };
    }

    private Task PublishNotificationEventsAsync(List<Notification> notifications)
    {
        var subscriber = Redis.GetSubscriber();
        var tasks = new List<Task>();

        foreach (var notification in notifications)
        {
            var channel = RedisChannel.Literal($"notifications:{notification.UserId}");
            var payload = new JobNotificationEvent(notification.Id, false);
            var json = JsonSerializer.Serialize(payload, ActivityHandlerSerializerContext.Default.JobNotificationEvent);

            tasks.Add(subscriber.PublishAsync(channel, json));
        }

        return Task.WhenAll(tasks);
    }
}
