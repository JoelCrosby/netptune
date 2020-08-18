using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class TaskInGroupRepository : Repository<DataContext, ProjectTaskInBoardGroup, int>, ITaskInGroupRepository
    {
        public TaskInGroupRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<ProjectTaskInBoardGroup> GetProjectTaskInGroup(int taskId, int groupId)
        {
            return Entities.FirstOrDefaultAsync(entity =>
                entity.ProjectTaskId == taskId
                && entity.BoardGroupId == groupId);
        }

        public Task<List<ProjectTaskInBoardGroup>> GetProjectTasksInGroup(int groupId)
        {
            return Entities
                .Where(entity => entity.BoardGroupId == groupId)
                .OrderBy(entity => entity.SortOrder)
                .ToListAsync();
        }

        public Task<ProjectTaskInBoardGroup> GetProjectTaskInGroup(int taskId)
        {
            return Entities
                .Where(entity => entity.ProjectTaskId == taskId)
                .OrderBy(entity => entity.SortOrder)
                .FirstOrDefaultAsync();
        }
    }
}
