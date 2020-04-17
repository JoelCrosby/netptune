using Netptune.Core.Repositories.Common;
using Netptune.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface IBoardRepository : IRepository<Board, int>
    {
        Task<List<Board>> GetBoardsInProject(int projectId, bool includeGroups = false);

        Task<Board> GetDefaultBoardInProject(int projectId, bool includeGroups = false);

        Task<Board> DeleteBoard(int id, AppUser user);
    }
}
