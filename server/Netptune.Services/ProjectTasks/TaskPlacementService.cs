using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.ProjectTasks;

public sealed class TaskPlacementService : ITaskPlacementService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public TaskPlacementService(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public async Task<bool> Place(int taskId, BoardGroupTaskTarget target, CancellationToken cancellationToken = default)
    {
        var existing = await UnitOfWork.ProjectTasksInGroups.GetPlacementOnBoard(taskId, target.BoardId, cancellationToken);
        var isAlreadyInTarget = existing?.BoardGroupId == target.Id;

        if (isAlreadyInTarget)
        {
            return false;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteTasksFromBoard([taskId], target.BoardId, cancellationToken);
        await AddPlacements([taskId], target, cancellationToken);

        return true;
    }

    public async Task PlaceMany(IReadOnlyList<int> taskIds, BoardGroupTaskTarget target, CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteTasksFromBoard(taskIds, target.BoardId, cancellationToken);
        await AddPlacements(taskIds, target, cancellationToken);
    }

    public async Task ReplaceAllPlacements(IReadOnlyList<int> taskIds, BoardGroupTaskTarget target, CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(taskIds, cancellationToken);
        await AddPlacements(taskIds, target, cancellationToken);
    }

    public async Task<TaskBoardMoves> FollowStatus(
        IReadOnlyCollection<int> taskIds,
        Status status,
        int? skipBoardId = null,
        CancellationToken cancellationToken = default)
    {
        var placements = await UnitOfWork.ProjectTasksInGroups.GetBoardPlacements(taskIds, cancellationToken);
        var placementsOutsideStatusGroups = placements
            .Where(placement => placement.BoardId != skipBoardId)
            .GroupBy(placement => (placement.BoardId, placement.TaskId))
            .Where(taskOnBoard => taskOnBoard.All(placement => placement.GroupStatusId != status.Id))
            .Select(taskOnBoard => taskOnBoard.First())
            .ToList();

        if (placementsOutsideStatusGroups.Count == 0)
        {
            return TaskBoardMoves.None;
        }

        var boardIds = placementsOutsideStatusGroups.Select(placement => placement.BoardId).Distinct().ToList();
        var targets = await UnitOfWork.BoardGroups.GetStatusTaskTargets(boardIds, status.Id, cancellationToken);
        var placementsToMove = placementsOutsideStatusGroups
            .Where(placement => targets.ContainsKey(placement.BoardId))
            .ToList();

        if (placementsToMove.Count == 0)
        {
            return TaskBoardMoves.None;
        }

        var placementIds = placementsToMove.Select(placement => placement.Id).ToList();
        var moves = placementsToMove
            .Select(placement => new TaskBoardMove(placement.TaskId, placement.BoardGroupId, targets[placement.BoardId]))
            .ToList();
        var newPlacements = moves
            .GroupBy(move => move.Target)
            .SelectMany(target => BuildPlacements(target.Select(move => move.TaskId).ToList(), target.Key))
            .ToList();

        await UnitOfWork.ProjectTasksInGroups.DeletePlacements(placementIds, cancellationToken);
        await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(newPlacements, cancellationToken);

        return new TaskBoardMoves(status, moves);
    }

    public async Task RecordMoves(
        TaskBoardMoves moves,
        TaskBoardMoveContext context,
        CancellationToken cancellationToken = default)
    {
        if (!moves.HasMoves)
        {
            return;
        }

        var events = moves.Moves
            .Select(move => BuildMoveEvent(move, moves.CausedByStatus, context))
            .ToList();

        await EventRecords.AppendRange(events, cancellationToken);
    }

    public async Task<bool> RemoveFromBoard(int taskId, int boardId, CancellationToken cancellationToken = default)
    {
        var existing = await UnitOfWork.ProjectTasksInGroups.GetPlacementOnBoard(taskId, boardId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteTasksFromBoard([taskId], boardId, cancellationToken);

        return true;
    }

    public async Task<BoardGroupTaskTarget?> ResolveEntryTarget(int boardId, int statusId, CancellationToken cancellationToken = default)
    {
        var statusTarget = await UnitOfWork.BoardGroups.GetStatusTaskTarget(boardId, statusId, cancellationToken);

        if (statusTarget is not null)
        {
            return statusTarget;
        }

        return await UnitOfWork.BoardGroups.GetFallbackTaskTarget(boardId, cancellationToken: cancellationToken);
    }

    private async Task AddPlacements(
        IReadOnlyList<int> taskIds,
        BoardGroupTaskTarget target,
        CancellationToken cancellationToken)
    {
        var placements = BuildPlacements(taskIds, target);

        await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(placements, cancellationToken);
    }

    private static IEnumerable<ProjectTaskInBoardGroup> BuildPlacements(
        IReadOnlyList<int> taskIds,
        BoardGroupTaskTarget target)
    {
        var baseSortOrder = target.MaxSortOrder + 1;

        return taskIds.Select((taskId, index) => new ProjectTaskInBoardGroup
        {
            ProjectTaskId = taskId,
            BoardGroupId = target.Id,
            SortOrder = baseSortOrder + index,
        });
    }

    private static EventWriteRequest<MoveTaskActivityMeta> BuildMoveEvent(
        TaskBoardMove move,
        Status? causedByStatus,
        TaskBoardMoveContext context)
    {
        var joinedBoard = move.FromGroupId is null;
        var references = new List<EventReferenceInput>
        {
            new()
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Board),
                EntityId = move.Target.BoardId.ToString(),
            },
        };

        if (move.Target.ProjectId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = move.Target.ProjectId.Value.ToString(),
            });
        }

        return new EventWriteRequest<MoveTaskActivityMeta>
        {
            WorkspaceId = context.WorkspaceId,
            EventKey = EventKeys.TaskBoardMoved,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = move.TaskId.ToString(),
            ActorUserId = context.ActorUserId,
            CorrelationId = context.CorrelationId,
            CausationEventId = context.CausationEventId,
            OriginType = context.OriginType,
            References = references,
            Payload = new MoveTaskActivityMeta
            {
                Group = move.Target.Name,
                GroupId = move.Target.Id,
                BoardId = joinedBoard ? move.Target.BoardId : null,
                FromGroupId = move.FromGroupId,
                CausedByStatusId = causedByStatus?.Id,
                CausedByStatus = causedByStatus?.Name,
            },
        };
    }
}
