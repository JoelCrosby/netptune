using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface IBoardRepository : IRepository<Board, int>
    {
        Task<List<Board>> GetBoardsInProject(int projectId, bool includeGroups = false);

        Task<Board> GetDefaultBoardInProject(int projectId, bool includeGroups = false);

        Task<List<Board>> GetBoards(string slug);
    }
}
