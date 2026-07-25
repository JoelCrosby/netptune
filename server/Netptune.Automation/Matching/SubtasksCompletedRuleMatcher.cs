using Microsoft.Extensions.Logging;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class SubtasksCompletedRuleMatcher : TaskDependencyRuleMatcher
{
    public override AutomationTriggerType TriggerType => AutomationTriggerType.SubtasksCompleted;

    protected override RelationCategory Category => RelationCategory.Hierarchy;

    public SubtasksCompletedRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<SubtasksCompletedRuleMatcher> logger)
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

        var parentTaskIds = await UnitOfWork.ProjectTaskRelations.GetParentTaskIds(
            [changedTaskId],
            cancellationToken);

        return await FilterFullyComplete(parentTaskIds, cancellationToken);
    }

    protected override async Task<List<int>> ResolveAffectedTaskIds(
        TaskRelationChangedMessage message,
        CancellationToken cancellationToken)
    {
        return await FilterFullyComplete([message.SourceTaskId], cancellationToken);
    }

    private async Task<List<int>> FilterFullyComplete(
        List<int> parentTaskIds,
        CancellationToken cancellationToken)
    {
        if (parentTaskIds.Count == 0)
        {
            return [];
        }

        var childCounts = await UnitOfWork.ProjectTaskRelations.GetChildCounts(parentTaskIds, cancellationToken);

        return childCounts
            .Where(counts => counts.Total > 0 && counts.Incomplete == 0)
            .Select(counts => counts.TaskId)
            .ToList();
    }
}
