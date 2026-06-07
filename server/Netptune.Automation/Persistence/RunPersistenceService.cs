using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence;

internal sealed class RunPersistenceService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<RunPersistenceService> Logger;

    public RunPersistenceService(
        INetptuneUnitOfWork unitOfWork,
        ILogger<RunPersistenceService> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<Notification>> Persist(
        AutomationTriggerType triggerType,
        List<AutomationRun> runs,
        List<NotificationActivityPlan> notificationPlans,
        List<Flag> flags,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var activityLogs = notificationPlans.Select(plan => plan.Activity).ToList();
        activity?.SetTag("automation.flags.created", flags.Count);
        activity?.SetTag("automation.activity_logs.created", activityLogs.Count);

        Logger.LogInformation(
            "Persisting automation results for trigger {TriggerType}: {RunCount} runs, {ActivityLogCount} activity logs, {FlagCount} flags",
            triggerType,
            runs.Count,
            activityLogs.Count,
            flags.Count);

        List<Notification> notifications = [];

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.ActivityLogs.AddRangeAsync(activityLogs, cancellationToken);
            await UnitOfWork.Flags.AddRangeAsync(flags, cancellationToken);
            await UnitOfWork.Automations.AddRunsAsync(runs, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            notifications = BuildNotifications(notificationPlans);
            activity?.SetTag("automation.notifications.created", notifications.Count);

            await UnitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        Logger.LogInformation(
            "Persisted automation results for trigger {TriggerType}: {NotificationCount} notifications",
            triggerType,
            notifications.Count);

        return notifications;
    }

    private static List<Notification> BuildNotifications(List<NotificationActivityPlan> activityPlans)
    {
        return activityPlans
            .SelectMany(plan =>
            {
                var task = plan.Execution.Task;
                var actorUserId = plan.Execution.ActorUserId;
                var link = BuildTaskLink(task);

                return plan.RecipientUserIds.Select(userId => new Notification
                {
                    UserId = userId,
                    ActivityLogId = plan.Activity.Id,
                    IsRead = false,
                    Link = link,
                    WorkspaceId = task.WorkspaceId,
                    EntityType = EntityType.Task,
                    ActivityType = ActivityType.Modify,
                    CreatedByUserId = actorUserId,
                    OwnerId = actorUserId,
                });
            })
            .ToList();
    }

    private static string BuildTaskLink(ProjectTask task)
    {
        var identifier = task.Project is null
            ? task.Id.ToString()
            : $"{task.Project.Key}-{task.ProjectScopeId}";

        return $"/{task.Workspace.Slug}/tasks/{identifier}";
    }
}
