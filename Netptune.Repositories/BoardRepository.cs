using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Models.Enums;
using Netptune.Repositories.Common;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Repositories
{
    public class BoardRepository : Repository<DataContext, Board, int>, IBoardRepository
    {
        public BoardRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public Task<List<Board>> GetBoardsInProject(int projectId, bool includeGroups = false)
        {
            var query = Entities
                .Where(board => board.ProjectId == projectId)
                .Where(board => !board.IsDeleted);

            if (!includeGroups) return query.ToListAsync();

            return query.Include(board => board.BoardGroups).ToListAsync();
        }
        
        public Task<Board> GetDefaultBoardInProject(int projectId, bool includeGroups = false)
        {
            var query = Entities
                .Where(board => board.ProjectId == projectId)
                .Where(board => !board.IsDeleted)
                .Where(board => board.BoardType == BoardType.Default);
          
            if (!includeGroups) return query.FirstOrDefaultAsync();

            return query.Include(board => board.BoardGroups).FirstOrDefaultAsync();
        }

        public async Task<Board> DeleteBoard(int id, AppUser user)
        {
            var board = await Entities.FindAsync(id);

            if (board is null) return null;

            board.IsDeleted = true;
            board.DeletedByUserId = user.Id;

            return board;
        }
    }
}
