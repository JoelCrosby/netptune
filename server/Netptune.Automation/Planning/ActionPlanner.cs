using System.Text.Json;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Configuration;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Automation.Planning;

internal sealed class ActionPlanner
{
    private readonly ILogger<ActionPlanner> Logger;

    public ActionPlanner(ILogger<ActionPlanner> logger)
    {
        Logger = logger;
    }

    internal ActionPlan Plan(List<PendingAutomationExecution> executions)
    {
        var runs = new List<AutomationRun>(executions.Count);
        var notificationPlans = new List<NotificationActivityPlan>();
        var flagPlans = new List<FlagPlan>();
        var taskUpdatePlans = new List<TaskUpdatePlan>();

        foreach (var execution in executions)
        {
            var rule = execution.Rule;
            var task = execution.Task;
            var run = new AutomationRun
            {
                AutomationRuleId = rule.Id,
                EntityType = EntityType.Task,
                EntityId = task.Id,
                TriggerType = rule.TriggerType,
                Status = AutomationRunStatus.Succeeded,
                IdempotencyKey = execution.IdempotencyKey,
                OwnerId = execution.ActorUserId,
                CreatedByUserId = execution.ActorUserId,
                Context = JsonSerializer.SerializeToDocument(new
                {
                    taskId = task.Id,
                    taskName = task.Name,
                    ruleId = rule.Id,
                    ruleName = rule.Name,
                }, JsonOptions.Default),
            };

            try
            {
                foreach (var action in rule.Actions.Where(action => !action.IsDeleted).OrderBy(action => action.SortOrder))
                {
                    switch (action.Type)
                    {
                        case AutomationActionType.NotifyTaskAssignees:
                            AddNotificationPlan(notificationPlans, execution, action);
                            break;
                        case AutomationActionType.FlagTask:
                            flagPlans.Add(new FlagPlan(execution, action));
                            break;
                        case AutomationActionType.UpdateTask:
                            AddTaskUpdatePlan(taskUpdatePlans, execution, action);
                            break;
                        default:
                            Logger.LogWarning(
                                "Automation rule {RuleId} has unsupported action type {ActionType}",
                                rule.Id,
                                action.Type);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                run.Status = AutomationRunStatus.Failed;
                run.Message = ex.Message;

                Logger.LogError(ex, "Automation rule {RuleId} failed for task {TaskId}", rule.Id, task.Id);
            }

            runs.Add(run);
        }

        return new ActionPlan
        {
            Runs = runs,
            NotificationPlans = notificationPlans,
            FlagPlans = flagPlans,
            TaskUpdatePlans = taskUpdatePlans,
        };
    }

    private void AddTaskUpdatePlan(
        List<TaskUpdatePlan> plans,
        PendingAutomationExecution execution,
        AutomationAction action)
    {
        var statusId = ConfigReader.ReadInt(action.Config, "statusId");
        var priority = ConfigReader.ReadEnum<TaskPriority>(action.Config, "priority");

        if (statusId is null && priority is null)
        {
            Logger.LogDebug(
                "Automation rule {RuleId} skipped task update action for task {TaskId}: no task fields configured",
                execution.Rule.Id,
                execution.Task.Id);
            return;
        }

        plans.Add(new TaskUpdatePlan
        {
            Execution = execution,
            Action = action,
            StatusId = statusId,
            Priority = priority,
        });
    }

    private void AddNotificationPlan(
        List<NotificationActivityPlan> plans,
        PendingAutomationExecution execution,
        AutomationAction action)
    {
        var task = execution.Task;
        var rule = execution.Rule;
        var recipientIds = task.ProjectTaskAppUsers
            .Select(assignee => assignee.UserId)
            .DefaultIfEmpty(task.OwnerId)
            .SelectMany(ToPresentUserId)
            .Distinct()
            .ToList();

        if (recipientIds.Count == 0)
        {
            Logger.LogDebug(
                "Automation rule {RuleId} skipped notification action for task {TaskId}: no recipients",
                rule.Id,
                task.Id);
            return;
        }

        var activity = new ActivityLog
        {
            OwnerId = execution.ActorUserId,
            Type = ActivityType.Modify,
            EntityType = EntityType.Task,
            EntityId = task.Id,
            UserId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
            WorkspaceId = task.WorkspaceId,
            WorkspaceSlug = task.Workspace.Slug,
            ProjectId = task.ProjectId,
            ProjectSlug = task.Project?.Key,
            TaskId = task.Id,
            OccurredAt = DateTime.UtcNow,
            Meta = JsonSerializer.SerializeToDocument(new
            {
                automationRuleId = rule.Id,
                automationRuleName = rule.Name,
                message = ConfigReader.ReadString(action.Config, "message") ?? $"Automation '{rule.Name}' matched this task.",
            }, JsonOptions.Default),
        };

        plans.Add(new NotificationActivityPlan
        {
            Execution = execution,
            Activity = activity,
            RecipientUserIds = recipientIds,
        });

        Logger.LogDebug(
            "Automation rule {RuleId} planned notification activity for task {TaskId} with {RecipientCount} recipients",
            rule.Id,
            task.Id,
            recipientIds.Count);
    }

    private static IEnumerable<string> ToPresentUserId(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            yield return userId;
        }
    }
}
