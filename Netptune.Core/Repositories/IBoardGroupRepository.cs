using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;

namespace Netptune.Core.Repositories
{
    public interface IBoardGroupRepository : IRepository<BoardGroup, int>
    {
        Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId);

        Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId);
    }
}
