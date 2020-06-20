using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;

namespace Netptune.Core.Services
{
    public interface IBoardGroupService
    {
        Task<List<BoardGroup>> GetBoardGroups(int boardId);

        ValueTask<BoardGroup> GetBoardGroup(int id);

        Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> AddBoardGroup(AddBoardGroupRequest boardGroup);

        Task<BoardGroup> DeleteBoardGroup(int id);
    }
}
