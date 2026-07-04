using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
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
        List<TaskUpdatePlan> taskUpdatePlans,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var activityLogs = notificationPlans.Select(plan => plan.Activity).ToList();
        activity?.SetTag("automation.flags.created", flags.Count);
        activity?.SetTag("automation.activity_logs.created", activityLogs.Count);
        activity?.SetTag("automation.task_updates.planned", taskUpdatePlans.Count);

        Logger.LogInformation(
            "Persisting automation results for trigger {TriggerType}: {RunCount} runs, {ActivityLogCount} activity logs, {FlagCount} flags, {TaskUpdateCount} task updates",
            triggerType,
            runs.Count,
            activityLogs.Count,
            flags.Count,
            taskUpdatePlans.Count);

        List<Notification> notifications = [];

        await UnitOfWork.Transaction(async () =>
        {
            await ApplyTaskUpdates(taskUpdatePlans, cancellationToken);
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

    private async Task ApplyTaskUpdates(
        List<TaskUpdatePlan> taskUpdatePlans,
        CancellationToken cancellationToken)
    {
        if (taskUpdatePlans.Count == 0) return;

        var tasks = new Dictionary<int, ProjectTask>();
        var updatedTaskIds = new HashSet<int>();

        foreach (var plan in taskUpdatePlans)
        {
            var taskId = plan.Execution.Task.Id;

            if (!tasks.TryGetValue(taskId, out var task))
            {
                task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

                if (task is null)
                {
                    Logger.LogWarning(
                        "Automation rule {RuleId} could not update missing task {TaskId}",
                        plan.Execution.Rule.Id,
                        taskId);
                    continue;
                }

                tasks[taskId] = task;
            }

            var taskUpdated = false;

            var status = plan.StatusId.HasValue
                ? await UnitOfWork.Statuses.GetInWorkspace(
                    plan.StatusId.Value,
                    plan.Execution.Rule.WorkspaceId,
                    cancellationToken: cancellationToken)
                : null;

            if (status is not null && task.StatusId != status.Id)
            {
                await UnitOfWork.Tasks.UpdateTaskStatus(taskId, status.Id, cancellationToken);
                task.StatusId = status.Id;
                task.Status = status;
                taskUpdated = true;
            }

            if (plan.Priority.HasValue && task.Priority != plan.Priority.Value)
            {
                task.Priority = plan.Priority.Value;
                taskUpdated = true;
            }

            if (taskUpdated)
            {
                updatedTaskIds.Add(taskId);
                task.UpdatedAt = DateTime.UtcNow;
                task.ModifiedByUserId = plan.Execution.ActorUserId;
            }
        }

        Activity.Current?.SetTag("automation.task_updates.applied", updatedTaskIds.Count);

        Logger.LogInformation(
            "Applied automation task updates to {UpdatedTaskCount} tasks",
            updatedTaskIds.Count);
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
