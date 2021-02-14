using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Services
{
    public class ActivityService : ServiceBase<ActivityViewModel>, IActivityService
    {
        private readonly IActivityLogRepository ActivityLogs;

        public ActivityService(INetptuneUnitOfWork unitOfWork)
        {
            ActivityLogs = unitOfWork.ActivityLogs;
        }

        public async Task<ClientResponse<List<ActivityViewModel>>> GetActivities(EntityType entityType, int entityId)
        {
            var result = await ActivityLogs.GetActivities(entityType, entityId);

            return Success(result);
        }
    }
}
