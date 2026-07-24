using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class OverdueTaskRuleMatcher : TaskStateRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.TaskOverdue;

    public OverdueTaskRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<OverdueTaskRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }

    protected override Task<List<ProjectTask>> GetCandidates(
        List<AutomationRule> rules,
        List<int> workspaceIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var latestToday = rules
            .Select(rule => AutomationTimeZones.Today(rule, now))
            .Max();

        return UnitOfWork.Tasks.GetOverdueAutomationCandidates(workspaceIds, latestToday, cancellationToken);
    }

    protected override bool MatchesTrigger(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var today = AutomationTimeZones.Today(rule, now);
        var isOverdue = task.DueDate < today;

        return isOverdue;
    }

    protected override string GetStateKey(ProjectTask task)
    {
        return $"due:{task.DueDate:yyyy-MM-dd}";
    }
}
