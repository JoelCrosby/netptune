using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Core.Services
{
    public interface IActivityService
    {
        Task<ClientResponse<List<ActivityViewModel>>> GetActivities(EntityType entityType, int entityId);
    }
}
