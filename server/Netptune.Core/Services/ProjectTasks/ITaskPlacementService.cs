using Netptune.Core.Entities;
using Netptune.Core.Models.ProjectTasks;

namespace Netptune.Core.Services.ProjectTasks;

public interface ITaskPlacementService
{
    Task<bool> Place(int taskId, BoardGroupTaskTarget target, CancellationToken cancellationToken = default);

    Task PlaceMany(IReadOnlyList<int> taskIds, BoardGroupTaskTarget target, CancellationToken cancellationToken = default);

    Task ReplaceAllPlacements(IReadOnlyList<int> taskIds, BoardGroupTaskTarget target, CancellationToken cancellationToken = default);

    Task<TaskBoardMoves> FollowStatus(IReadOnlyCollection<int> taskIds, Status status, int? skipBoardId = null, CancellationToken cancellationToken = default);

    Task RecordMoves(TaskBoardMoves moves, TaskBoardMoveContext context, CancellationToken cancellationToken = default);

    Task<bool> RemoveFromBoard(int taskId, int boardId, CancellationToken cancellationToken = default);

    Task<BoardGroupTaskTarget?> ResolveEntryTarget(int boardId, int statusId, CancellationToken cancellationToken = default);
}
