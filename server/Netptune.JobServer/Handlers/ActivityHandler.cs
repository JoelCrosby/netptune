using System.Text.Json;
using System.Text.Json.Serialization;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.UnitOfWork;

using StackExchange.Redis;

namespace Netptune.JobServer.Handlers;

[JsonSerializable(typeof(JobNotificationEvent))]
internal partial class ActivityHandlerSerializerContext : JsonSerializerContext;

public record JobNotificationEvent(int NotificationId, bool IsRead);

public sealed class ActivityHandler : IRequestHandler<ActivityMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IConnectionMultiplexer Redis;

    public ActivityHandler(INetptuneUnitOfWork unitOfWork, IConnectionMultiplexer redis)
    {
        UnitOfWork = unitOfWork;
        Redis = redis;
    }

    public async ValueTask<Unit> Handle(ActivityMessage request, CancellationToken cancellationToken)
    {
        var activityLogs = new List<(ActivityLog Log, ActivityAncestors Ancestors)>();

        foreach (var activity in request.Events)
        {
            if (activity.EntityId is null)
            {
                throw new Exception("IActivityEvent EntityId cannot be null.");
            }

            var ancestors = activity.EntityType switch
            {
                EntityType.Task => await UnitOfWork.Ancestors.GetProjectTaskAncestors(activity.EntityId.Value),
                EntityType.BoardGroup => await UnitOfWork.Ancestors.GetBoardGroupAncestors(activity.EntityId.Value),
                EntityType.Board => await UnitOfWork.Ancestors.GetBoardAncestors(activity.EntityId.Value),
                EntityType.Project => await UnitOfWork.Ancestors.GetProjectAncestors(activity.EntityId.Value),
                _ => new ActivityAncestors(),
            };

            var log = new ActivityLog
            {
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
                Time = activity.Time,
                Meta = activity.Meta is not null ? JsonDocument.Parse(activity.Meta) : null,
                BoardSlug = ancestors.BoardKey,
                ProjectSlug = ancestors.ProjectKey,
                WorkspaceSlug = ancestors.WorkspaceKey,
            };

            await UnitOfWork.ActivityLogs.AddAsync(log);
            activityLogs.Add((log, ancestors));
        }

        await UnitOfWork.CompleteAsync();

        await CreateNotificationsAsync(activityLogs);

        return default;
    }

    private async Task CreateNotificationsAsync(List<(ActivityLog Log, ActivityAncestors Ancestors)> activityLogs)
    {
        var workspaceIds = activityLogs.Select(l => l.Log.WorkspaceId).Distinct().ToList();

        var slugsByWorkspace = await UnitOfWork.Workspaces.GetSlugsByIds(workspaceIds);
        var usersByWorkspace = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIdsByWorkspaceIds(workspaceIds);

        var allNotifications = new List<Notification>();

        foreach (var (log, ancestors) in activityLogs)
        {
            if (!slugsByWorkspace.TryGetValue(log.WorkspaceId, out var slug)) continue;
            if (!usersByWorkspace.TryGetValue(log.WorkspaceId, out var allUserIds)) continue;

            var recipients = allUserIds.Where(id => id != log.UserId).ToList();
            if (recipients.Count == 0) continue;

            var link = BuildLink(slug, log, ancestors);

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

    private static string BuildLink(string workspaceSlug, ActivityLog log, ActivityAncestors ancestors)
    {
        return log.EntityType switch
        {
            EntityType.Task when ancestors.ProjectKey is not null => $"/{workspaceSlug}/tasks/{ancestors.ProjectKey}-{ancestors.TaskScopeId}",
            EntityType.Task => $"/{workspaceSlug}/tasks/{ancestors.TaskId}",
            EntityType.Board => $"/{workspaceSlug}/boards/{ancestors.BoardKey}",
            EntityType.Project => $"/{workspaceSlug}/projects/{log.EntityId}",
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
