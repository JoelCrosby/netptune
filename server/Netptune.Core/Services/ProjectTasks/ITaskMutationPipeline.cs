using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Services.ProjectTasks;

public sealed record TaskMutationRequest
{
    public required TaskViewModel Previous { get; init; }

    public required TaskViewModel Current { get; init; }

    public required ProjectTaskDiff Diff { get; init; }

    public required string ActorUserId { get; init; }

    public EventOriginType OriginType { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public int? AutomationRuleId { get; init; }

    public int? AutomationRunId { get; init; }

    public int ChainDepth { get; init; }
}

public sealed record TaskMutationOutcome(TaskMutationRequest Mutation, TaskChangedMessage? Message);

public sealed record TaskMutationValues(Status? Status, TaskPriority? Priority);

public interface ITaskMutationPipeline
{
    bool Apply(ProjectTask task, TaskMutationValues values);

    Task<TaskMutationOutcome> Record(TaskMutationRequest request, CancellationToken cancellationToken = default);

    Task Publish(TaskMutationOutcome outcome);
}
