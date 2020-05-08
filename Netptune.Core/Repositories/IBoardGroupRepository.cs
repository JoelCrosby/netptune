using Netptune.Core.Repositories.Common;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface IBoardGroupRepository : IRepository<BoardGroup, int>
    {
        Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId);

        Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId);

        Task<List<ProjectTask>> GetTasksInGroup(int groupId);
    }
}
