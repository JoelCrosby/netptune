using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class BoardGroupRepository : Repository<DataContext, BoardGroup, int>, IBoardGroupRepository
    {
        public BoardGroupRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId)
        {
            return Entities

                .Where(boardGroup => boardGroup.BoardId == boardId)
                .Where(boardGroup => !boardGroup.IsDeleted)

                .OrderBy(boardGroup => boardGroup.SortOrder)

                .Include(group => group.TasksInGroups)
                    .ThenInclude(relational => relational.ProjectTask)
                        .ThenInclude(task => task.Owner)

                .Include(group => group.TasksInGroups)
                    .ThenInclude(relational => relational.ProjectTask)
                        .ThenInclude(task => task.Project)

                .ToListAsync();
        }

        public Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId)
        {
            return Entities

                .Where(group => group.TasksInGroups
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.ProjectTaskId)
                    .Contains(taskId))

                .Include(group => group.TasksInGroups)
                    .ThenInclude(relational => relational.ProjectTask)

                .ToListAsync();
        }
    }
}
