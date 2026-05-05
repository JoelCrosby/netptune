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

    public Task<List<ProjectTaskInBoardGroup>> GetProjectTasksInGroup(int groupId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => entity.BoardGroupId == groupId)
            .OrderBy(entity => entity.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => entity.ProjectTaskId == taskId)
            .OrderBy(entity => entity.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<int>> GetAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();
        return await Entities
            .Where(entity => taskIdList.Contains(entity.ProjectTaskId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
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

    public async Task DeleteTaskFromGroup(int taskId, int groupId, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(entity => entity.ProjectTaskId == taskId && entity.BoardGroupId == groupId)
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
