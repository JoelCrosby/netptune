using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Core.Repositories
{
    public interface IActivityLogRepository : IWorkspaceEntityRepository<ActivityLog, int>
    {
        Task<List<ActivityViewModel>> GetActivities(EntityType entityType, int entityId);
    }
}
