using System.Text.Json;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Planning;

internal sealed class ActionPlanner
{
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly ILogger<ActionPlanner> Logger;

    public ActionPlanner(IAutomationActionRegistry actionRegistry, ILogger<ActionPlanner> logger)
    {
        ActionRegistry = actionRegistry;
        Logger = logger;
    }

    internal ActionPlan Plan(List<PendingAutomationExecution> executions)
    {
        var plan = new ActionPlan
        {
            Runs = new List<AutomationRun>(executions.Count),
            NotificationPlans = [],
            FlagPlans = [],
            TaskUpdatePlans = [],
            CommentPlans = [],
        };

        foreach (var execution in executions)
        {
            var run = CreateRun(execution);

            try
            {
                PlanActions(execution, plan);
            }
            catch (Exception ex)
            {
                run.Status = AutomationRunStatus.Failed;
                run.Message = ex.Message;

                Logger.LogError(ex, "Automation rule {RuleId} failed for task {TaskId}", execution.Rule.Id, execution.Task.Id);
            }

            plan.Runs.Add(run);
        }

        return plan;
    }

    private void PlanActions(PendingAutomationExecution execution, ActionPlan plan)
    {
        var actions = execution.Rule.Actions
            .Where(action => !action.IsDeleted)
            .OrderBy(action => action.SortOrder);

        foreach (var action in actions)
        {
            var automationAction = ActionRegistry.Find(action.Type);

            if (automationAction is null)
            {
                Logger.LogWarning("Automation rule {RuleId} has unsupported action type {ActionType}", execution.Rule.Id, action.Type);

                continue;
            }

            var context = new AutomationActionPlanningContext
            {
                Rule = execution.Rule,
                Action = action,
                Task = execution.Task,
                ActorUserId = execution.ActorUserId,
            };
            var contribution = automationAction.Plan(context);

            AddContribution(plan, execution, contribution);
        }
    }

    private static void AddContribution(
        ActionPlan plan,
        PendingAutomationExecution execution,
        AutomationActionPlanContribution contribution)
    {
        if (contribution.Notification is { } notification)
        {
            plan.NotificationPlans.Add(new NotificationActivityPlan
            {
                Execution = execution,
                Activity = notification.Activity,
                RecipientUserIds = notification.RecipientUserIds,
            });
        }

        if (contribution.Flag is { } flag)
        {
            plan.FlagPlans.Add(new FlagPlan
            {
                Execution = execution,
                Name = flag.Name,
                Description = flag.Description,
            });
        }

        if (contribution.TaskUpdate is { } taskUpdate)
        {
            plan.TaskUpdatePlans.Add(new TaskUpdatePlan
            {
                Execution = execution,
                StatusId = taskUpdate.StatusId,
                Priority = taskUpdate.Priority,
            });
        }

        if (contribution.CommentBody is { } commentBody)
        {
            plan.CommentPlans.Add(new CommentPlan(execution, commentBody));
        }
    }

    private static AutomationRun CreateRun(PendingAutomationExecution execution)
    {
        var rule = execution.Rule;
        var task = execution.Task;

        return new AutomationRun
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
    }
}
