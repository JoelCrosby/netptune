using Netptune.Core.Repositories.Common;
using Netptune.Models.Relationships;

using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskInGroupRepository : IAuditableRepository<ProjectTaskInBoardGroup, int>
    {
        Task<ProjectTaskInBoardGroup> GetProjectTaskInGroup(int taskId, int groupId);
    }
}
