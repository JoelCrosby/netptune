using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Execution;
using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class TaskChangedRuleMatcher : IEventRuleMatcher<TaskChangedMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ScheduledActionService ScheduledActions;
    private readonly ILogger<TaskChangedRuleMatcher> Logger;

    public AutomationTriggerType TriggerType => AutomationTriggerType.TaskChanged;

    public TaskChangedRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ScheduledActionService scheduledActions,
        ILogger<TaskChangedRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        ScheduledActions = scheduledActions;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(
        TaskChangedMessage message,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var changedFields = string.Join(",", message.Changes.Select(change => change.Field));

        activity?.SetTag("task.id", message.TaskId);
        activity?.SetTag("workspace.id", message.WorkspaceId);
        activity?.SetTag("automation.event_id", message.EventId.ToString());
        activity?.SetTag("automation.origin_type", message.OriginType.ToString());
        activity?.SetTag("automation.correlation_id", message.CorrelationId?.ToString());
        activity?.SetTag("automation.chain_depth", message.ChainDepth);
        activity?.SetTag("automation.changed_fields", changedFields);

        await ScheduledActions.CancelForStatusChange(message, cancellationToken);

        var chainLimitReached = AutomationChainPolicy.HasReachedLimit(message.ChainDepth);

        if (chainLimitReached)
        {
            Logger.LogWarning(
                "Task-change automation evaluation stopped at chain depth {ChainDepth} for task {TaskId} ({CorrelationId})",
                message.ChainDepth,
                message.TaskId,
                message.CorrelationId);
            activity?.SetTag("automation.skip_reason", "chain_depth_limit");
            Telemetry.RecordRulesSkipped(TriggerType, 1, "chain_depth_limit");

            return [];
        }

        Logger.LogInformation(
            "Evaluating task-change automation rules for task {TaskId} in workspace {WorkspaceId} ({EventId})",
            message.TaskId,
            message.WorkspaceId,
            message.EventId);

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            message.WorkspaceId,
            cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);

        activity?.SetTag("automation.rules.evaluated", rules.Count);

        if (rules.Count == 0)
        {
            Logger.LogDebug(
                "No task-change automation rules found for workspace {WorkspaceId}",
                message.WorkspaceId);

            return [];
        }

        var task = await UnitOfWork.Tasks.GetAutomationTask(message.TaskId, cancellationToken);

        if (task is null)
        {
            Logger.LogWarning(
                "Task-change automation skipped for missing or deleted task {TaskId}",
                message.TaskId);
            activity?.SetTag("automation.skip_reason", "task_not_found");
            Telemetry.RecordRulesSkipped(TriggerType, rules.Count, "task_not_found");

            return [];
        }

        var executions = new List<PendingAutomationExecution>();
        var correlationId = message.CorrelationId ?? message.EventId;
        var selfTriggerSkippedCount = 0;

        foreach (var rule in rules)
        {
            var isAutomationEvent = message.OriginType == EventOriginType.Automation;
            var isSourceRule = message.AutomationRuleId == rule.Id;
            var isSelfTrigger = isAutomationEvent && isSourceRule;

            if (isSelfTrigger)
            {
                selfTriggerSkippedCount++;

                Logger.LogWarning(
                    "Automation rule {RuleId} skipped its own task-change event {EventId} at chain depth {ChainDepth}",
                    rule.Id,
                    message.EventId,
                    message.ChainDepth);

                continue;
            }

            if (!TaskChangedRuleConditions.Match(rule, message, task))
            {
                continue;
            }

            executions.Add(new PendingAutomationExecution
            {
                Rule = rule,
                Task = task,
                ExecutionUserId = rule.ExecutionUserId,
                InitiatingUserId = message.ActorUserId,
                IdempotencyKey = $"rule:{rule.Id}:task:{message.TaskId}:event:{message.EventId}",
                TriggeredAt = message.OccurredAt,
                CorrelationId = correlationId,
                CausationEventId = message.EventId,
                ChainDepth = message.ChainDepth,
                TriggerMessage = message,
            });
        }

        if (selfTriggerSkippedCount > 0)
        {
            Telemetry.RecordRulesSkipped(
                TriggerType,
                selfTriggerSkippedCount,
                "self_trigger");
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} of {RuleCount} task-change automation rules for task {TaskId}",
            executions.Count,
            rules.Count,
            message.TaskId);

        return executions;
    }
}
