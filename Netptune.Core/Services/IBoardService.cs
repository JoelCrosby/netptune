using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services
{
    public interface IBoardService
    {
        Task<List<Board>> GetBoards(int projectId);

        ValueTask<Board> GetBoard(int id);

        Task<Board> UpdateBoard(Board board);

        Task<Board> AddBoard(Board board);

        Task<Board> DeleteBoard(int id);

        Task<List<BoardViewModel>> GetBoardsInWorkspace(string slug);
    }
}
