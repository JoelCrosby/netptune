using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IBoardService
    {
        Task<List<Board>> GetBoards(int projectId);

        Task<Board> GetBoard(int id);

        Task<Board> UpdateBoard(Board board);

        Task<Board> AddBoard(Board board);

        Task<Board> DeleteBoard(int id, AppUser user);
    }
}
