using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Services.Relations;

public sealed record TaskRelationPlanRequest
{
    public required int WorkspaceId { get; init; }

    public required string WorkspaceKey { get; init; }

    public required IReadOnlyCollection<AddTaskRelationRequest> Links { get; init; }
}

public sealed record PlannedTaskRelation(RelationType RelationType, TaskViewModel Task, bool TaskIsSource);

public sealed record LinkedTaskRelation(ProjectTaskRelation Relation, RelationCategory Category);

public sealed record TaskRelationPlan
{
    public int WorkspaceId { get; init; }

    public IReadOnlyList<PlannedTaskRelation> Relations { get; init; } = [];

    public string Error { get; init; } = string.Empty;

    public bool IsValid => Error.Length == 0;

    public static TaskRelationPlan Failed(string error)
    {
        return new TaskRelationPlan { Error = error };
    }
}

// Links a batch of task relations in the three stages the write demands: Plan before anything is
// written, Apply inside the caller's transaction, Publish once it has committed. Plan assumes the
// task the links attach to has no relations of its own yet, which is what lets a whole batch be
// validated before the task row exists.
public interface ITaskRelationLinker
{
    Task<TaskRelationPlan> Plan(TaskRelationPlanRequest request, CancellationToken cancellationToken = default);

    Task<List<LinkedTaskRelation>> Apply(TaskRelationPlan plan, int taskId, CancellationToken cancellationToken = default);

    Task Publish(IReadOnlyCollection<LinkedTaskRelation> links, string actorUserId);
}
