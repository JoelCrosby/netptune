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
        var ids = await Entities
            .Where(entity => taskIdList.Contains(entity.ProjectTaskId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        await DeletePermanent(ids, cancellationToken);
    }
}
