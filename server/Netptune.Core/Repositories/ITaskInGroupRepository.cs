using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskInGroupRepository : IRepository<ProjectTaskInBoardGroup, int>
{
    Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, int groupId);

    Task<List<ProjectTaskInBoardGroup>> GetProjectTasksInGroup(int groupId);

    Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId);

    Task<List<int>> GetAllByTaskId(IEnumerable<int> taskIds);

    Task DeleteAllByTaskId(IEnumerable<int> taskIds);
}
