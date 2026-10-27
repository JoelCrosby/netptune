using System.Text.Json;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
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

    public List<AiTaskPlacementSnapshot> Placements { get; init; } = [];

    // The task as the change left it. Absent on snapshots taken before it was recorded.
    public AiTaskSnapshot? Applied { get; init; }
}

public sealed record AiTaskPlacementSnapshot
{
    public int BoardId { get; init; }

    public int BoardGroupId { get; init; }
}

[Flags]
internal enum AiTaskFacets
{
    None = 0,
    Name = 1,
    Description = 2,
    Status = 4,
    Assignees = 8,
    Tags = 16,
    Priority = 32,
    Estimate = 64,
    StartDate = 128,
    DueDate = 256,
    Sprint = 512,
    Placements = 1024,
    Fields = Name | Description | Status | Assignees | Tags | Priority | Estimate | StartDate | DueDate,
    All = Fields | Sprint | Placements,
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

        return JsonSerializer.SerializeToDocument(ToSnapshot(task), SerializerOptions);
    }

    public static async Task<JsonDocument?> CaptureApplied(
        IMediator mediator,
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var before = Read(context.Change.UndoPayload);

        if (before is null)
        {
            return context.Change.UndoPayload;
        }

        var task = await mediator.Send(new GetTaskQuery(before.TaskId), cancellationToken);

        if (task is null)
        {
            return context.Change.UndoPayload;
        }

        var snapshot = before with { Applied = ToSnapshot(task) };

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
            return AiChangePayload.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var current = await mediator.Send(new GetTaskQuery(snapshot.TaskId), cancellationToken);

        if (current is null)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorTaskCouldNotRestored);
        }

        var applied = snapshot.Applied;
        var touched = applied is null ? AiTaskFacets.All : Differences(snapshot, applied);

        if (applied is not null)
        {
            var changedSince = Differences(applied, ToSnapshot(current)) & touched;

            if (changedSince != AiTaskFacets.None)
            {
                return AiChangePayload.Failure(change, AppText.AiChangeErrorTaskChangedSinceChangeAppliedLeftNow);
            }
        }

        var placementError = await RestoreSprintAndPlacements(mediator, snapshot, current, touched, cancellationToken);

        if (placementError is not null)
        {
            return AiChangePayload.Failure(change, placementError);
        }

        var request = CreateRestoreRequest(snapshot, touched);

        if (request is not null)
        {
            var response = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

            if (!response.IsSuccess)
            {
                var fieldError = response.Message ?? AppText.AiChangeErrorTaskCouldNotRestored;

                return AiChangePayload.Failure(change, fieldError);
            }
        }

        return AiChangePayload.Applied(change, snapshot.TaskId);
    }

    private static AiTaskFacets Differences(AiTaskSnapshot left, AiTaskSnapshot right)
    {
        var facets = AiTaskFacets.None;

        facets |= Differs(left.Name != right.Name, AiTaskFacets.Name);
        facets |= Differs(left.Description != right.Description, AiTaskFacets.Description);
        facets |= Differs(left.StatusId != right.StatusId, AiTaskFacets.Status);
        facets |= Differs(!SameSet(left.AssigneeIds, right.AssigneeIds), AiTaskFacets.Assignees);
        facets |= Differs(!SameSet(left.Tags, right.Tags), AiTaskFacets.Tags);
        facets |= Differs(left.Priority != right.Priority, AiTaskFacets.Priority);

        var estimateDiffers = left.EstimateType != right.EstimateType || left.EstimateValue != right.EstimateValue;

        facets |= Differs(estimateDiffers, AiTaskFacets.Estimate);
        facets |= Differs(left.StartDate != right.StartDate, AiTaskFacets.StartDate);
        facets |= Differs(left.DueDate != right.DueDate, AiTaskFacets.DueDate);
        facets |= Differs(left.SprintId != right.SprintId, AiTaskFacets.Sprint);
        facets |= Differs(!SamePlacements(left.Placements, right.Placements), AiTaskFacets.Placements);

        return facets;
    }

    private static AiTaskFacets Differs(bool differs, AiTaskFacets facet)
    {
        return differs ? facet : AiTaskFacets.None;
    }

    private static bool SameSet(List<string> left, List<string> right)
    {
        return left.ToHashSet(StringComparer.Ordinal).SetEquals(right);
    }

    private static bool SamePlacements(List<AiTaskPlacementSnapshot> left, List<AiTaskPlacementSnapshot> right)
    {
        return left.ToHashSet().SetEquals(right);
    }

    private static AiTaskSnapshot ToSnapshot(TaskViewModel task)
    {
        return new AiTaskSnapshot
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
            Placements = task.Placements.ConvertAll(placement => new AiTaskPlacementSnapshot
            {
                BoardId = placement.BoardId,
                BoardGroupId = placement.BoardGroupId,
            }),
        };
    }

    // Moving a task between board groups can set its status from the group, so the fields go back
    // after the placements, or the move would overwrite the status just restored.
    private static async Task<string?> RestoreSprintAndPlacements(
        IMediator mediator,
        AiTaskSnapshot snapshot,
        TaskViewModel current,
        AiTaskFacets touched,
        CancellationToken cancellationToken)
    {
        if (touched.HasFlag(AiTaskFacets.Sprint))
        {
            var sprintError = await RestoreSprint(mediator, snapshot, current.SprintId, cancellationToken);

            if (sprintError is not null)
            {
                return sprintError;
            }
        }

        if (touched.HasFlag(AiTaskFacets.Placements))
        {
            return await RestorePlacements(mediator, snapshot, current, cancellationToken);
        }

        return null;
    }

    private static UpdateProjectTaskRequest? CreateRestoreRequest(AiTaskSnapshot snapshot, AiTaskFacets touched)
    {
        var restoresFields = (touched & AiTaskFacets.Fields) != AiTaskFacets.None;

        if (!restoresFields)
        {
            return null;
        }

        var restoresPriority = touched.HasFlag(AiTaskFacets.Priority);
        var restoresEstimate = touched.HasFlag(AiTaskFacets.Estimate);
        var request = new UpdateProjectTaskRequest
        {
            Id = snapshot.TaskId,
            Name = touched.HasFlag(AiTaskFacets.Name) ? snapshot.Name : null,
            Description = touched.HasFlag(AiTaskFacets.Description) ? snapshot.Description ?? string.Empty : null,
            StatusId = touched.HasFlag(AiTaskFacets.Status) ? snapshot.StatusId : null,
            AssigneeIds = touched.HasFlag(AiTaskFacets.Assignees) ? snapshot.AssigneeIds : null,
            Tags = touched.HasFlag(AiTaskFacets.Tags) ? snapshot.Tags : null,
            Priority = restoresPriority ? snapshot.Priority : null,
            ClearPriority = restoresPriority && snapshot.Priority is null,
            EstimateType = restoresEstimate ? snapshot.EstimateType : null,
            EstimateValue = restoresEstimate ? snapshot.EstimateValue : null,
            ClearEstimate = restoresEstimate && snapshot.EstimateType is null && snapshot.EstimateValue is null,
        };

        if (touched.HasFlag(AiTaskFacets.StartDate))
        {
            request.StartDate = snapshot.StartDate;
        }

        if (touched.HasFlag(AiTaskFacets.DueDate))
        {
            request.DueDate = snapshot.DueDate;
        }

        return request;
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

            return response.IsSuccess ? null : response.Message ?? AppText.AiChangeErrorTaskCouldNotPutBackSprint;
        }

        var removal = await mediator.Send(
            new RemoveTaskFromSprintCommand(currentSprintId!.Value, snapshot.TaskId),
            cancellationToken);

        return removal.IsSuccess ? null : removal.Message ?? AppText.AiChangeErrorTaskCouldNotTakenBackOutSprint;
    }

    private static async Task<string?> RestorePlacements(
        IMediator mediator,
        AiTaskSnapshot snapshot,
        TaskViewModel current,
        CancellationToken cancellationToken)
    {
        // Payloads captured before placements were recorded name one board group and say nothing about
        // the boards the task was on, so they can only put that one group back.
        var isLegacyPayload = snapshot.Placements.Count == 0;

        if (isLegacyPayload)
        {
            return await RestoreSingleBoardGroup(mediator, snapshot, current, cancellationToken);
        }

        foreach (var placement in snapshot.Placements)
        {
            var error = await RestorePlacement(mediator, snapshot.TaskId, placement, current, cancellationToken);

            if (error is not null)
            {
                return error;
            }
        }

        var snapshotBoardIds = snapshot.Placements.Select(placement => placement.BoardId).ToHashSet();
        var addedBoardIds = current.Placements
            .Select(placement => placement.BoardId)
            .Where(boardId => !snapshotBoardIds.Contains(boardId))
            .ToList();

        foreach (var boardId in addedBoardIds)
        {
            var response = await mediator.Send(
                new RemoveTaskFromBoardCommand(snapshot.TaskId, boardId),
                cancellationToken);

            if (!response.IsSuccess)
            {
                return response.Message ?? AppText.AiChangeErrorTaskCouldNotTakenBackOffBoard;
            }
        }

        return null;
    }

    private static async Task<string?> RestorePlacement(
        IMediator mediator,
        int taskId,
        AiTaskPlacementSnapshot placement,
        TaskViewModel current,
        CancellationToken cancellationToken)
    {
        var currentGroupId = current.Placements
            .Where(existing => existing.BoardId == placement.BoardId)
            .Select(existing => (int?)existing.BoardGroupId)
            .FirstOrDefault();

        if (currentGroupId == placement.BoardGroupId)
        {
            return null;
        }

        var response = await mediator.Send(
            new MoveTasksToBoardGroupCommand([taskId], placement.BoardGroupId),
            cancellationToken);

        return response.IsSuccess ? null : response.Message ?? AppText.AiChangeErrorTaskCouldNotPutBackBoardGroup;
    }

    private static async Task<string?> RestoreSingleBoardGroup(
        IMediator mediator,
        AiTaskSnapshot snapshot,
        TaskViewModel current,
        CancellationToken cancellationToken)
    {
        var isAlreadyPlaced = current.Placements.Any(placement => placement.BoardGroupId == snapshot.BoardGroupId);

        if (isAlreadyPlaced || !snapshot.BoardGroupId.HasValue)
        {
            return null;
        }

        var response = await mediator.Send(
            new MoveTasksToBoardGroupCommand([snapshot.TaskId], snapshot.BoardGroupId.Value),
            cancellationToken);

        return response.IsSuccess ? null : response.Message ?? AppText.AiChangeErrorTaskCouldNotPutBackBoardGroup;
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
