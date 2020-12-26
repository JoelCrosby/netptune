using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Repositories
{
    public interface IBoardGroupRepository : IRepository<BoardGroup, int>
    {
        Task<BoardGroup> GetWithTasksInGroups(int id);

        Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false);

        Task<List<BoardViewGroup>> GetBoardView(int boardId, string searchTerm = null);

        Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId, bool isReadonly = false);

        Task<List<ProjectTask>> GetTasksInGroup(int groupId, bool isReadonly = false);

        ValueTask<double> GetBoardGroupDefaultSortOrder(int boardId);
    }
}
