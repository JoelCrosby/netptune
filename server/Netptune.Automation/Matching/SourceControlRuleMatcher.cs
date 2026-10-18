using System.Diagnostics;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.SourceControl;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal abstract class SourceControlRuleMatcher : IEventRuleMatcher<SourceControlTaskEventMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public abstract AutomationTriggerType TriggerType { get; }

    protected SourceControlRuleMatcher(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<List<PendingAutomationExecution>> Match(
        SourceControlTaskEventMessage message,
        CancellationToken cancellationToken)
    {
        var isThisTrigger = message.TriggerType == TriggerType;

        if (!isThisTrigger || message.TaskIds.Count == 0)
        {
            return [];
        }

        var activity = Activity.Current;

        activity?.SetTag("workspace.id", message.WorkspaceId);
        activity?.SetTag("automation.event_id", message.EventId.ToString());

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            message.WorkspaceId,
            cancellationToken);

        var matchingRules = rules.Where(rule => MatchesTriggerSettings(rule, message)).ToList();

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        if (matchingRules.Count == 0)
        {
            return [];
        }

        var tasks = await UnitOfWork.Tasks.GetAutomationTasks(message.TaskIds, cancellationToken);
        var workspaceTasks = tasks.Where(task => task.WorkspaceId == message.WorkspaceId).ToList();

        var executions = matchingRules
            .SelectMany(rule => workspaceTasks
                .Where(task => AutomationRuleConditions.Match(rule, task))
                .Select(task => ToExecution(rule, task, message)))
            .ToList();

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);

        return executions;
    }

    protected virtual bool MatchesTriggerSettings(AutomationRule rule, SourceControlTaskEventMessage message)
    {
        return true;
    }

    protected static bool MatchesTargetBranch(AutomationRule rule, SourceControlTaskEventMessage message)
    {
        var targetBranches = JsonUtils.ReadList<string>(rule.TriggerConfig, "targetBranches");

        return AutomationSourceControlTrigger.MatchesBranch(targetBranches, message.TargetBranch);
    }

    private PendingAutomationExecution ToExecution(
        AutomationRule rule,
        ProjectTask task,
        SourceControlTaskEventMessage message)
    {
        var trigger = TriggerType.ToString().ToLowerInvariant();

        return new PendingAutomationExecution
        {
            Rule = rule,
            Task = task,
            ExecutionUserId = rule.ExecutionUserId,
            InitiatingUserId = message.ActorUserId,
            IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:scm:{trigger}:{message.EventId}",
            TriggeredAt = message.OccurredAt,
            CorrelationId = message.EventId,
            CausationEventId = message.EventId,
        };
    }
}

internal sealed class PullRequestOpenedRuleMatcher : SourceControlRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.PullRequestOpened;

    public PullRequestOpenedRuleMatcher(INetptuneUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    protected override bool MatchesTriggerSettings(AutomationRule rule, SourceControlTaskEventMessage message)
    {
        var includeDrafts = JsonUtils.ReadObject<bool?>(rule.TriggerConfig, "includeDrafts") ?? false;
        // A rule that takes drafts already ran when the draft opened.
        var alreadyRanForDraft = includeDrafts && message.IsReadyForReview;
        var isAcceptedState = (includeDrafts || !message.IsDraft) && !alreadyRanForDraft;

        return isAcceptedState && MatchesTargetBranch(rule, message);
    }
}

internal sealed class PullRequestMergedRuleMatcher : SourceControlRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.PullRequestMerged;

    public PullRequestMergedRuleMatcher(INetptuneUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    protected override bool MatchesTriggerSettings(AutomationRule rule, SourceControlTaskEventMessage message)
    {
        return MatchesTargetBranch(rule, message);
    }
}

internal sealed class ChecksFailedRuleMatcher : SourceControlRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.ChecksFailed;

    public ChecksFailedRuleMatcher(INetptuneUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }
}
