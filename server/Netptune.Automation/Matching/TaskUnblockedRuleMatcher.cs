using Microsoft.Extensions.Logging;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class TaskUnblockedRuleMatcher : TaskDependencyRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.TaskUnblocked;

    protected override RelationCategory Category => RelationCategory.Dependency;

    public TaskUnblockedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<TaskUnblockedRuleMatcher> logger)
        : base(unitOfWork, logger)
    {
    }

    protected override async Task<List<int>> ResolveAffectedTaskIds(
        StatusTransition transition,
        int changedTaskId,
        CancellationToken cancellationToken)
    {
        if (!transition.BecameComplete)
        {
            return [];
        }

        var dependentTaskIds = await UnitOfWork.ProjectTaskRelations.GetDependentTaskIds(
            [changedTaskId],
            cancellationToken);

        return await FilterNewlyUnblocked(dependentTaskIds, cancellationToken);
    }

    protected override async Task<List<int>> ResolveAffectedTaskIds(
        TaskRelationChangedMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Change != TaskRelationChange.Removed)
        {
            return [];
        }

        return await FilterNewlyUnblocked([message.TargetTaskId], cancellationToken);
    }

    private async Task<List<int>> FilterNewlyUnblocked(
        List<int> taskIds,
        CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        var blockerCounts = await UnitOfWork.ProjectTaskRelations.GetBlockerCounts(taskIds, cancellationToken);
        var countsByTask = blockerCounts.ToDictionary(counts => counts.TaskId);

        return taskIds
            .Where(taskId => HasNoRemainingBlockers(countsByTask, taskId))
            .ToList();
    }

    private static bool HasNoRemainingBlockers(Dictionary<int, TaskRelationCounts> countsByTask, int taskId)
    {
        var hasCounts = countsByTask.TryGetValue(taskId, out var counts);

        return !hasCounts || counts!.Incomplete == 0;
    }
}
