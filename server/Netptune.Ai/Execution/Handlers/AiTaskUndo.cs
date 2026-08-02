using System.Text.Json;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed record AiTaskSnapshot
{
    public int TaskId { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int StatusId { get; init; }

    public List<string> AssigneeIds { get; init; } = [];

    public List<string> Tags { get; init; } = [];

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public int? SprintId { get; init; }

    public int? BoardGroupId { get; init; }
}

public static class AiTaskUndo
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<JsonDocument?> Capture(
        IMediator mediator,
        int? taskId,
        CancellationToken cancellationToken)
    {
        if (!taskId.HasValue)
        {
            return null;
        }

        var task = await mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return null;
        }

        var snapshot = new AiTaskSnapshot
        {
            TaskId = task.Id,
            Name = task.Name,
            Description = task.Description,
            StatusId = task.StatusId,
            AssigneeIds = task.Assignees.Select(assignee => assignee.Id).ToList(),
            Tags = task.Tags,
            Priority = task.Priority,
            EstimateType = task.EstimateType,
            EstimateValue = task.EstimateValue,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            SprintId = task.SprintId,
            BoardGroupId = task.BoardGroupId,
        };

        return JsonSerializer.SerializeToDocument(snapshot, SerializerOptions);
    }

    public static async Task<AiAppliedChangeResult> Restore(
        IMediator mediator,
        AiChangeUndoContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = Read(change.UndoPayload);

        if (snapshot is null)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var request = new UpdateProjectTaskRequest
        {
            Id = snapshot.TaskId,
            Name = snapshot.Name,
            Description = snapshot.Description ?? string.Empty,
            StatusId = snapshot.StatusId,
            AssigneeIds = snapshot.AssigneeIds,
            Tags = snapshot.Tags,
            Priority = snapshot.Priority,
            EstimateType = snapshot.EstimateType,
            EstimateValue = snapshot.EstimateValue,
            StartDate = snapshot.StartDate,
            DueDate = snapshot.DueDate,
        };

        var response = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The task could not be restored.");
        }

        var current = await mediator.Send(new GetTaskQuery(snapshot.TaskId), cancellationToken);

        if (current is null)
        {
            return AiChangeUndoResult.Failure(change, "The task could not be read back after restoring it.");
        }

        var sprintError = await RestoreSprint(mediator, snapshot, current.SprintId, cancellationToken);

        if (sprintError is not null)
        {
            return AiChangeUndoResult.Failure(change, sprintError);
        }

        var groupError = await RestoreBoardGroup(mediator, snapshot, current.BoardGroupId, cancellationToken);

        if (groupError is not null)
        {
            return AiChangeUndoResult.Failure(change, groupError);
        }

        return AiChangeUndoResult.Undone(change, snapshot.TaskId);
    }

    private static async Task<string?> RestoreSprint(
        IMediator mediator,
        AiTaskSnapshot snapshot,
        int? currentSprintId,
        CancellationToken cancellationToken)
    {
        var isUnchanged = currentSprintId == snapshot.SprintId;

        if (isUnchanged)
        {
            return null;
        }

        if (snapshot.SprintId.HasValue)
        {
            var request = new AddTasksToSprintRequest { TaskIds = [snapshot.TaskId] };
            var response = await mediator.Send(
                new AddTasksToSprintCommand(snapshot.SprintId.Value, request),
                cancellationToken);

            return response.IsSuccess ? null : response.Message ?? "The task could not be put back in its sprint.";
        }

        var removal = await mediator.Send(
            new RemoveTaskFromSprintCommand(currentSprintId!.Value, snapshot.TaskId),
            cancellationToken);

        return removal.IsSuccess ? null : removal.Message ?? "The task could not be taken back out of the sprint.";
    }

    private static async Task<string?> RestoreBoardGroup(
        IMediator mediator,
        AiTaskSnapshot snapshot,
        int? currentBoardGroupId,
        CancellationToken cancellationToken)
    {
        var isUnchanged = currentBoardGroupId == snapshot.BoardGroupId;

        if (isUnchanged || !snapshot.BoardGroupId.HasValue)
        {
            return null;
        }

        var response = await mediator.Send(
            new MoveTasksToBoardGroupCommand([snapshot.TaskId], snapshot.BoardGroupId.Value),
            cancellationToken);

        return response.IsSuccess ? null : response.Message ?? "The task could not be put back in its board group.";
    }

    private static AiTaskSnapshot? Read(JsonDocument? payload)
    {
        if (payload is null)
        {
            return null;
        }

        try
        {
            return payload.Deserialize<AiTaskSnapshot>(SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
