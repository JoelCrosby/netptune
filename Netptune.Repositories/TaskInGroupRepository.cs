using Microsoft.EntityFrameworkCore;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

using System.Threading.Tasks;

namespace Netptune.Repositories
{
    public class TaskInGroupRepository : AuditableRepository<DataContext, ProjectTaskInBoardGroup, int>, ITaskInGroupRepository
    {
        public TaskInGroupRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<ProjectTaskInBoardGroup> GetProjectTaskInGroup(int taskId, int groupId)
        {
            return Entities.FirstOrDefaultAsync(entity =>
                entity.ProjectTaskId == taskId
                && entity.BoardGroupId == groupId
                && !entity.IsDeleted);
        }
    }
}