using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class NoDueDateTaskRuleMatcher : TaskStateRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.TaskHasNoDueDate;

    public NoDueDateTaskRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<NoDueDateTaskRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }

    protected override Task<List<ProjectTask>> GetCandidates(
        List<AutomationRule> rules,
        List<int> workspaceIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return UnitOfWork.Tasks.GetNoDueDateAutomationCandidates(workspaceIds, cancellationToken);
    }

    protected override bool MatchesTrigger(AutomationRule rule, ProjectTask task, DateTime now)
    {
        return true;
    }

    protected override string GetStateKey(ProjectTask task)
    {
        return "missing-due-date";
    }
}
