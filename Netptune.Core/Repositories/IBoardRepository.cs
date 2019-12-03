using System.Collections.Generic;
using System.Threading.Tasks;
using Netptune.Core.Repositories.Common;
using Netptune.Models;

namespace Netptune.Core.Repositories
{
    public interface IBoardRepository : IRepository<Board, int>
    {
        Task<List<Board>> GetBoardsInProject(int projectId);

        Task<Board> DeleteBoard(int id, AppUser user);
    }
}
