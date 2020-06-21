using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface ITaskInGroupRepository : IAuditableRepository<ProjectTaskInBoardGroup, int>
    {
        Task<ProjectTaskInBoardGroup> GetProjectTaskInGroup(int taskId, int groupId);

        Task<List<ProjectTaskInBoardGroup>> GetProjectTasksInGroup(int groupId);
    }
}
