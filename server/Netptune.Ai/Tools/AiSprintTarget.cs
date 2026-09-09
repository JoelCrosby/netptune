using System.Text.Json;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Ai.Tools;

internal sealed record AiSprintTarget
{
    public required string Name { get; init; }

    public int? Id { get; init; }

    public string? RefKey { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectName { get; init; }
}

internal sealed record AiSprintTargetResult(AiSprintTarget? Sprint, string? Error)
{
    public static AiSprintTargetResult Found(AiSprintTarget sprint)
    {
        return new AiSprintTargetResult(sprint, null);
    }

    public static AiSprintTargetResult Failed(string error)
    {
        return new AiSprintTargetResult(null, error);
    }
}

internal static class AiSprintTargetLookup
{
    public static async Task<AiSprintTargetResult> Resolve(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var sprintRef = AiPendingReference.Read(arguments, "sprintRef");

        if (sprintRef is not null)
        {
            return FindPending(changeSet, sprintRef);
        }

        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiSprintTargetResult.Failed(
                "A sprintId is required, or a sprintRef for a sprint proposed in this change set.");
        }

        var sprint = await AiSprintLookup.Find(mediator, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return AiSprintTargetResult.Failed($"Sprint {sprintId} is not in this workspace.");
        }

        var isCompleted = sprint.Status == SprintStatus.Completed;

        if (isCompleted)
        {
            return AiSprintTargetResult.Failed($"Sprint “{sprint.Name}” is completed and can no longer be changed.");
        }

        return AiSprintTargetResult.Found(new AiSprintTarget
        {
            Name = sprint.Name,
            Id = sprint.Id,
            ProjectId = sprint.ProjectId,
            ProjectName = sprint.ProjectName,
        });
    }

    // A task that already exists cannot belong to a project this change set has not created yet, so a
    // pending sprint only takes existing tasks when it was proposed in an existing project.
    public static string? FindProjectMismatch(AiSprintTarget sprint, TaskViewModel task)
    {
        var isProjectPending = sprint.ProjectId is null;

        if (isProjectPending)
        {
            return $"Task {task.SystemId} cannot join sprint “{sprint.Name}”, whose project is also proposed in this change set.";
        }

        var belongsToProject = task.ProjectId == sprint.ProjectId;

        if (belongsToProject)
        {
            return null;
        }

        return $"Task {task.SystemId} is not in project {sprint.ProjectName}.";
    }

    private static AiSprintTargetResult FindPending(IAiChangeSetBuilder changeSet, string sprintRef)
    {
        var pending = AiPendingReference.Find(changeSet, sprintRef, "sprint");

        if (pending is null)
        {
            return AiSprintTargetResult.Failed(AiPendingReference.Missing(sprintRef, "sprint"));
        }

        var project = pending.Fields.FirstOrDefault(field => string.Equals(field.Name, "project", StringComparison.Ordinal));

        return AiSprintTargetResult.Found(new AiSprintTarget
        {
            Name = AiPendingReference.ProposedName(pending),
            RefKey = sprintRef,
            ProjectId = AiToolSchema.GetInt(pending.Payload.RootElement, "projectId"),
            ProjectName = project?.After,
        });
    }
}
