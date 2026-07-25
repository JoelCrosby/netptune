using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Events.Sprints;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal abstract class SprintLifecycleRuleMatcher : IEventRuleMatcher<SprintLifecycleMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger Logger;

    public abstract AutomationTriggerType TriggerType { get; }

    protected abstract SprintLifecycleState State { get; }

    protected SprintLifecycleRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(
        SprintLifecycleMessage message,
        CancellationToken cancellationToken)
    {
        if (message.State != State)
        {
            return [];
        }

        var activity = Activity.Current;

        activity?.SetTag("sprint.id", message.SprintId);
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

        var tasks = await UnitOfWork.Tasks.GetSprintAutomationTasks(message.SprintId, cancellationToken);

        activity?.SetTag("automation.tasks.candidate", tasks.Count);

        if (tasks.Count == 0)
        {
            Logger.LogDebug(
                "{TriggerType} automation found no tasks in sprint {SprintId}",
                TriggerType,
                message.SprintId);

            return [];
        }

        var executions = new List<PendingAutomationExecution>();

        foreach (var rule in rules)
        {
            var matchingTasks = tasks.Where(task => AutomationRuleConditions.Match(rule, task));

            foreach (var task in matchingTasks)
            {
                executions.Add(new PendingAutomationExecution
                {
                    Rule = rule,
                    Task = task,
                    ExecutionUserId = rule.ExecutionUserId,
                    InitiatingUserId = message.ActorUserId,
                    IdempotencyKey = BuildIdempotencyKey(rule.Id, task.Id, message),
                    TriggeredAt = message.OccurredAt,
                    CorrelationId = message.EventId,
                    CausationEventId = message.EventId,
                });
            }
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);

        return executions;
    }

    private static string BuildIdempotencyKey(int ruleId, int taskId, SprintLifecycleMessage message)
    {
        var state = message.State.ToString().ToLowerInvariant();

        return $"rule:{ruleId}:task:{taskId}:sprint:{message.SprintId}:{state}:{message.EventId}";
    }
}

internal sealed class SprintStartedRuleMatcher : SprintLifecycleRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.SprintStarted;

    protected override SprintLifecycleState State => SprintLifecycleState.Started;

    public SprintStartedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<SprintStartedRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }
}

internal sealed class SprintCompletedRuleMatcher : SprintLifecycleRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.SprintCompleted;

    protected override SprintLifecycleState State => SprintLifecycleState.Completed;

    public SprintCompletedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<SprintCompletedRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }
}
