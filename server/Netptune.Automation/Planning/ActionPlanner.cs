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
            Actions = [],
        };

        var orderedExecutions = executions
            .OrderBy(execution => execution.Rule.Id)
            .ThenBy(execution => execution.Task.Id);

        foreach (var execution in orderedExecutions)
        {
            var run = CreateRun(execution);
            execution.Run = run;

            try
            {
                var actions = PlanActions(execution);

                plan.Actions.AddRange(actions);
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

    private List<PlannedAutomationAction> PlanActions(PendingAutomationExecution execution)
    {
        var actions = execution.Rule.Actions
            .Where(action => !action.IsDeleted)
            .OrderBy(action => action.SortOrder)
            .ThenBy(action => action.Id)
            .ToList();

        var plannedActions = new List<PlannedAutomationAction>();

        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var result = CreateActionResult(execution, action);

            execution.Run!.ActionResults.Add(result);

            var automationAction = ActionRegistry.Find(action.Type);

            if (automationAction is null)
            {
                Logger.LogWarning("Automation rule {RuleId} has unsupported action type {ActionType}", execution.Rule.Id, action.Type);
                CompleteActionResult(result, AutomationActionResultStatus.Skipped, "The action type is not supported.", markStarted: true);

                continue;
            }

            try
            {
                var context = new AutomationActionPlanningContext
                {
                    Rule = execution.Rule,
                    Action = action,
                    Task = execution.Task,
                    ActorUserId = execution.ActorUserId,
                };
                var contribution = automationAction.Plan(context);

                plannedActions.Add(new PlannedAutomationAction
                {
                    Execution = execution,
                    Action = action,
                    Contribution = contribution,
                    Result = result,
                });
            }
            catch (Exception ex)
            {
                CompleteActionResult(result, AutomationActionResultStatus.Failed, ex.Message, markStarted: true);
                SkipUnexecutedResults(execution, result, actions, index + 1);

                throw;
            }
        }

        return plannedActions;
    }

    private static AutomationActionResult CreateActionResult(PendingAutomationExecution execution, AutomationAction action)
    {
        return new AutomationActionResult
        {
            AutomationRun = execution.Run!,
            AutomationActionId = action.Id,
            ActionType = action.Type,
            SortOrder = action.SortOrder,
            Status = AutomationActionResultStatus.Pending,
            IdempotencyKey = $"{execution.IdempotencyKey}:action:{action.Id}",
            OwnerId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
        };
    }

    private static void SkipUnexecutedResults(
        PendingAutomationExecution execution,
        AutomationActionResult failedResult,
        List<AutomationAction> actions,
        int remainingStartIndex)
    {
        foreach (var result in execution.Run!.ActionResults)
        {
            var isUnexecuted = result != failedResult && result.Status == AutomationActionResultStatus.Pending;

            if (isUnexecuted)
            {
                CompleteActionResult(
                    result,
                    AutomationActionResultStatus.Skipped,
                    "The run was cancelled because a later action could not be planned.");
            }
        }

        for (var index = remainingStartIndex; index < actions.Count; index++)
        {
            var result = CreateActionResult(execution, actions[index]);
            CompleteActionResult(
                result,
                AutomationActionResultStatus.Skipped,
                "The action was not planned because an earlier action failed.");
            execution.Run.ActionResults.Add(result);
        }
    }

    private static void CompleteActionResult(
        AutomationActionResult result,
        AutomationActionResultStatus status,
        string message,
        bool markStarted = false)
    {
        var completedAt = DateTime.UtcNow;

        if (markStarted)
        {
            result.StartedAt = completedAt;
        }

        result.Status = status;
        result.Message = message;
        result.CompletedAt = completedAt;
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
                correlationId = execution.CorrelationId,
                causationEventId = execution.CausationEventId,
                chainDepth = execution.ChainDepth,
            }, JsonOptions.Default),
        };
    }
}
