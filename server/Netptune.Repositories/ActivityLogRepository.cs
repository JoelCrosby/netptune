using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class ActivityLogRepository : WorkspaceEntityRepository<DataContext, ActivityLog, int>, IActivityLogRepository
    {
        public ActivityLogRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }
    }
}
