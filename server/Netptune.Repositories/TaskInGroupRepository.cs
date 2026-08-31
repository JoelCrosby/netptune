using Microsoft.EntityFrameworkCore;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class TaskInGroupRepository : Repository<DataContext, ProjectTaskInBoardGroup, int>, ITaskInGroupRepository
{
    public TaskInGroupRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, int groupId, CancellationToken cancellationToken = default)
    {
        return Entities.FirstOrDefaultAsync(entity =>
            entity.ProjectTaskId == taskId
            && entity.BoardGroupId == groupId, cancellationToken);
    }

    public Task<ProjectTaskInBoardGroup?> GetPlacementOnBoard(int taskId, int boardId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => entity.ProjectTaskId == taskId && entity.BoardGroup!.BoardId == boardId)
            .OrderBy(entity => entity.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetPlacementGroupsOnBoard(
        IReadOnlyCollection<int> taskIds,
        int boardId,
        CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        var placements = await Entities
            .AsNoTracking()
            .Where(entity => taskIds.Contains(entity.ProjectTaskId) && entity.BoardGroup!.BoardId == boardId)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new { entity.ProjectTaskId, entity.BoardGroupId })
            .ToListAsync(cancellationToken);

        return placements
            .GroupBy(placement => placement.ProjectTaskId)
            .ToDictionary(group => group.Key, group => group.First().BoardGroupId);
    }

    public async Task DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        if (taskIdList.Count == 0)
        {
            return;
        }

        await Entities
            .Where(entity => taskIdList.Contains(entity.ProjectTaskId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteTasksFromBoard(IEnumerable<int> taskIds, int boardId, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        if (taskIdList.Count == 0)
        {
            return;
        }

        await Entities
            .Where(entity => taskIdList.Contains(entity.ProjectTaskId) && entity.BoardGroup!.BoardId == boardId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task MovePlacementsToGroup(int fromGroupId, int toGroupId, double baseSortOrder, CancellationToken cancellationToken = default)
    {
        // The (group, task) alternate key rejects the update outright when the task already sits in
        // the target group, so those rows are dropped rather than merged.
        await Entities
            .Where(entity => entity.BoardGroupId == fromGroupId)
            .Where(entity => Entities.Any(target =>
                target.BoardGroupId == toGroupId && target.ProjectTaskId == entity.ProjectTaskId))
            .ExecuteDeleteAsync(cancellationToken);

        await Entities
            .Where(entity => entity.BoardGroupId == fromGroupId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.BoardGroupId, toGroupId)
                .SetProperty(entity => entity.SortOrder, entity => baseSortOrder + entity.SortOrder), cancellationToken);
    }

    public async Task DeletePlacementsInGroup(int groupId, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(entity => entity.BoardGroupId == groupId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<(double? Previous, double? Next)> GetNeighborSortOrdersForInsert(
        int groupId,
        int taskId,
        int currentIndex,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .AsNoTracking()
            .Where(entity => entity.BoardGroupId == groupId && entity.ProjectTaskId != taskId)
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .Select(entity => entity.SortOrder);

        var count = await query.CountAsync(cancellationToken);

        if (currentIndex < 0 || currentIndex > count)
        {
            throw new($"Get task in group sort order request '{nameof(currentIndex)}' is outside range of board group");
        }

        var previous = currentIndex == 0
            ? null
            : await query.Skip(currentIndex - 1).Select(sortOrder => (double?)sortOrder).FirstOrDefaultAsync(cancellationToken);

        var next = currentIndex >= count
            ? null
            : await query.Skip(currentIndex).Select(sortOrder => (double?)sortOrder).FirstOrDefaultAsync(cancellationToken);

        return (previous, next);
    }
}
