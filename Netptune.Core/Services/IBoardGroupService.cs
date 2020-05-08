using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IBoardGroupService
    {
        Task<List<BoardGroup>> GetBoardGroups(int boardId);

        Task<BoardGroup> GetBoardGroup(int id);

        Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> AddBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> DeleteBoardGroup(int id, AppUser user);
    }
}
