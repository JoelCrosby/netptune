using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Services;

public class ActivityService : ServiceBase<ActivityViewModel>, IActivityService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public ActivityService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async Task<ClientResponse<List<ActivityViewModel>>> GetActivities(EntityType entityType, int entityId)
    {
        var activities = await UnitOfWork.ActivityLogs.GetActivities(entityType, entityId);

        var workspaceId = await Identity.GetWorkspaceId();
        var assigneeIds = GetAssigneeIds(activities).ToHashSet();
        var avatars = await UnitOfWork.Users.GetUserAvatars(assigneeIds, workspaceId);
        var avatarMap = avatars.ToDictionary(k => k.Id, v => v);

        foreach (var activity in activities.Where(activity => activity.Meta is not null))
        {
            if (!activity.Meta.RootElement.TryGetProperty("assigneeId", out var element))
            {
                continue;
            }

            var assigneeId = element.GetString();

            if (assigneeId is {} && avatarMap.TryGetValue(assigneeId, out var avatar))
            {
                activity.Assignee = avatar;
            }
        }

        return Success(activities);
    }

    private static IEnumerable<string> GetAssigneeIds(IEnumerable<ActivityViewModel> activities)
    {
        foreach (var activity in activities)
        {
            if (activity.Meta?.RootElement is null)
            {
                continue;
            }

            if (activity.Meta.RootElement.TryGetProperty("assigneeId", out var element))
            {
                yield return element.GetString();
            }
        }
    }
}
