using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
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

        var executions = rules
            .Where(rule => AutomationRuleConditions.Match(rule, task))
            .Select(rule => new PendingAutomationExecution
            {
                Rule = rule,
                Task = task,
                ExecutionUserId = rule.ExecutionUserId,
                InitiatingUserId = message.ActorUserId,
                IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:created:{message.EventId}",
                TriggeredAt = message.OccurredAt,
                CorrelationId = message.EventId,
                CausationEventId = message.EventId,
            })
            .ToList();

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);

        return executions;
    }
}
