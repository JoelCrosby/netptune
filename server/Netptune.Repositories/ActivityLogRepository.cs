using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

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
            Expression<Func<ActivityLog, bool>> predicate = entityType switch
            {
                EntityType.Task => x => (x.EntityId == entityId || x.TaskId == entityId),
                EntityType.Board => x => (x.EntityId == entityId || x.BoardId == entityId),
                EntityType.Project => x => (x.EntityId == entityId || x.ProjectId == entityId),
                EntityType.Workspace => x => (x.EntityId == entityId || x.WorkspaceId == entityId),
                EntityType.BoardGroup => x => (x.EntityId == entityId || x.BoardGroupId == entityId),
                _ => x => true,
            };

            return Entities
                .Where(x => !x.IsDeleted && x.EntityType == entityType)
                .Where(predicate)
                .OrderByDescending(x => x.Time)
                .Include(x => x.User)
                .Select(y => new ActivityViewModel
                {
                    Type = y.Type,
                    EntityId = y.EntityId,
                    EntityType = entityType,
                    UserId = y.UserId,
                    UserUsername = y.User.DisplayName,
                    UserPictureUrl = y.User.PictureUrl,
                    Time = y.Time,
                })
                .ToReadonlyListAsync(true);
        }
    }
}
