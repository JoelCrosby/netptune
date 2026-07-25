using Microsoft.Extensions.Logging;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class TaskBlockedRuleMatcher : TaskDependencyRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.TaskBlocked;

    protected override RelationCategory Category => RelationCategory.Dependency;

    public TaskBlockedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<TaskBlockedRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }

    protected override async Task<List<int>> ResolveAffectedTaskIds(
        StatusTransition transition,
        int changedTaskId,
        CancellationToken cancellationToken)
    {
        if (!transition.BecameIncomplete)
        {
            return [];
        }

        var dependentTaskIds = await UnitOfWork.ProjectTaskRelations.GetDependentTaskIds([changedTaskId], cancellationToken);

        return await FilterNewlyBlocked(dependentTaskIds, cancellationToken);
    }

    protected override async Task<List<int>> ResolveAffectedTaskIds(
        TaskRelationChangedMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Change != TaskRelationChange.Added)
        {
            return [];
        }

        return await FilterNewlyBlocked([message.TargetTaskId], cancellationToken);
    }

    private async Task<List<int>> FilterNewlyBlocked(
        List<int> taskIds,
        CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        var blockerCounts = await UnitOfWork.ProjectTaskRelations.GetBlockerCounts(taskIds, cancellationToken);

        return blockerCounts
            .Where(counts => counts.Incomplete == 1)
            .Select(counts => counts.TaskId)
            .ToList();
    }
}
