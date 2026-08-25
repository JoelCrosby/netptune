using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.ProjectTasks;

public sealed class TaskPlacementService : ITaskPlacementService
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskPlacementService(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
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

    public async Task ReplaceAllPlacements(int taskId, BoardGroupTaskTarget target, CancellationToken cancellationToken = default)
    {
        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId([taskId], cancellationToken);
        await AddPlacements([taskId], target, cancellationToken);
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
        var baseSortOrder = target.MaxSortOrder + 1;
        var placements = taskIds.Select((taskId, index) => new ProjectTaskInBoardGroup
        {
            ProjectTaskId = taskId,
            BoardGroupId = target.Id,
            SortOrder = baseSortOrder + index,
        });

        await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(placements, cancellationToken);
    }
}
