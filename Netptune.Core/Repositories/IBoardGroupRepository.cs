using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface IBoardGroupRepository : IRepository<BoardGroup, int>
    {
        Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false);

        Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId, bool isReadonly = false);

        Task<List<ProjectTask>> GetTasksInGroup(int groupId, bool isReadonly = false);
    }
}
