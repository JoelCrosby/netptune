using Microsoft.Extensions.Logging;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class InactiveTaskRuleMatcher : TaskStateRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.TaskInactiveFor;

    public InactiveTaskRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<InactiveTaskRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }

    protected override Task<List<ProjectTask>> GetCandidates(
        List<AutomationRule> rules,
        List<int> workspaceIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var durations = rules
            .Select(ReadDurationDays)
            .Where(duration => duration.HasValue)
            .Select(duration => duration!.Value)
            .ToList();

        if (durations.Count == 0)
        {
            return Task.FromResult(new List<ProjectTask>());
        }

        var broadestCutoff = now.AddDays(-durations.Min());

        return UnitOfWork.Tasks.GetInactiveAutomationCandidates(workspaceIds, broadestCutoff, cancellationToken);
    }

    protected override bool MatchesTrigger(AutomationRule rule, ProjectTask task, DateTime now)
    {
        return AutomationTriggerPredicates.MatchesInactive(rule, task, now);
    }

    protected override string GetStateKey(ProjectTask task)
    {
        var lastActivityAt = task.UpdatedAt ?? task.CreatedAt;

        return $"inactive:{lastActivityAt:O}";
    }

    private static int? ReadDurationDays(AutomationRule rule)
    {
        var durationDays = JsonUtils.ReadInt(rule.TriggerConfig, "durationDays");
        var hasValidDuration = durationDays is >= 1 and <= 365;

        return hasValidDuration ? durationDays : null;
    }
}
