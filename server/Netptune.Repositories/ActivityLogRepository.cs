using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Activity;
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

        public Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId)
        {
            return Entities
                .Where(x => !x.IsDeleted && x.EntityType == entityType && x.EntityId == entityId)
                .OrderByDescending(x => x.Time)
                .Select(y => new ActivityViewModel
                {
                    Type = y.Type,
                    EntityId = y.EntityId,
                    EntityType = entityType,
                    UserId = y.UserId,
                })
                .ToReadonlyListAsync(true);
        }
    }
}
