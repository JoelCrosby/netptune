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

internal sealed class TaskCreatedRuleMatcher : IEventRuleMatcher<TaskCreatedMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<TaskCreatedRuleMatcher> Logger;

    public AutomationTriggerType TriggerType => AutomationTriggerType.TaskCreated;

    public TaskCreatedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<TaskCreatedRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(
        TaskCreatedMessage message,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        activity?.SetTag("task.id", message.TaskId);
        activity?.SetTag("workspace.id", message.WorkspaceId);
        activity?.SetTag("automation.event_id", message.EventId.ToString());
        activity?.SetTag("automation.origin_type", message.OriginType.ToString());
        activity?.SetTag("automation.chain_depth", message.ChainDepth);

        var chainLimitReached = AutomationChainPolicy.HasReachedLimit(message.ChainDepth);

        if (chainLimitReached)
        {
            Logger.LogWarning(
                "Task-created automation evaluation stopped at chain depth {ChainDepth} for task {TaskId} ({CorrelationId})",
                message.ChainDepth,
                message.TaskId,
                message.CorrelationId);
            activity?.SetTag("automation.skip_reason", "chain_depth_limit");
            Telemetry.RecordRulesSkipped(TriggerType, 1, "chain_depth_limit");

            return [];
        }

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            message.WorkspaceId,
            cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        if (rules.Count == 0)
        {
            return [];
        }

        var task = await UnitOfWork.Tasks.GetAutomationTask(message.TaskId, cancellationToken);

        if (task is null)
        {
            Logger.LogWarning("Task-created automation skipped missing task {TaskId}", message.TaskId);
            Telemetry.RecordRulesSkipped(TriggerType, rules.Count, "task_not_found");

            return [];
        }

        var isAutomationEvent = message.OriginType == EventOriginType.Automation;
        var selfTriggeringRules = rules
            .Where(rule => isAutomationEvent && message.AutomationRuleId == rule.Id)
            .ToList();

        if (selfTriggeringRules.Count > 0)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} skipped its own task-created event {EventId} at chain depth {ChainDepth}",
                selfTriggeringRules[0].Id,
                message.EventId,
                message.ChainDepth);
            Telemetry.RecordRulesSkipped(TriggerType, selfTriggeringRules.Count, "self_trigger");
        }

        var correlationId = message.CorrelationId ?? message.EventId;
        var executions = rules
            .Except(selfTriggeringRules)
            .Where(rule => AutomationRuleConditions.Match(rule, task))
            .Select(rule => new PendingAutomationExecution
            {
                Rule = rule,
                Task = task,
                ExecutionUserId = rule.ExecutionUserId,
                InitiatingUserId = message.ActorUserId,
                IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:created:{message.EventId}",
                TriggeredAt = message.OccurredAt,
                CorrelationId = correlationId,
                CausationEventId = message.EventId,
                ChainDepth = message.ChainDepth,
            })
            .ToList();

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);

        return executions;
    }
}
