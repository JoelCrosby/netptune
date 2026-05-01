using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskInGroupRepository : IRepository<ProjectTaskInBoardGroup, int>
{
    Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, int groupId, CancellationToken cancellationToken = default);

    Task<List<ProjectTaskInBoardGroup>> GetProjectTasksInGroup(int groupId, CancellationToken cancellationToken = default);

    Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, CancellationToken cancellationToken = default);

    Task<List<int>> GetAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);

    Task DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);
}
