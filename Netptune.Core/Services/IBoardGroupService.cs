using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services
{
    public interface IBoardGroupService
    {
        Task<BoardGroupsViewModel> GetBoardGroups(string boardIdentifier);

        Task<BoardGroupsViewModel> GetBoardGroups(int boardId);

        Task<BoardGroup> GetBoardGroup(int id);

        Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> AddBoardGroup(AddBoardGroupRequest boardGroup);

        Task<BoardGroup> DeleteBoardGroup(int id);
    }
}
