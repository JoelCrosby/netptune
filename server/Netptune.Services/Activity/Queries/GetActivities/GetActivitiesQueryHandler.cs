using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.Services.Activity.Queries.GetActivities;

public sealed class GetActivitiesQueryHandler : IRequestHandler<GetActivitiesQuery, ClientResponse<List<ActivityViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetActivitiesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<ActivityViewModel>>> Handle(GetActivitiesQuery request, CancellationToken cancellationToken)
    {
        var activities = await UnitOfWork.ActivityLogs.GetActivities(request.EntityType, request.EntityId);

        var workspaceId = await Identity.GetWorkspaceId();
        var assigneeIds = GetAssigneeIds(activities).ToHashSet();
        var avatars = await UnitOfWork.Users.GetUserAvatars(assigneeIds, workspaceId);
        var avatarMap = avatars.ToDictionary(k => k.Id, v => v);

        foreach (var activity in activities.Where(a => a.Meta is not null))
        {
            if (!activity.Meta!.RootElement.TryGetProperty("assigneeId", out var element)) continue;

            var assigneeId = element.GetString();

            if (assigneeId is not null && avatarMap.TryGetValue(assigneeId, out var avatar))
            {
                activity.Assignee = avatar;
            }
        }

        return activities;
    }

    private static IEnumerable<string> GetAssigneeIds(IEnumerable<ActivityViewModel> activities)
    {
        foreach (var activity in activities)
        {
            if (activity.Meta?.RootElement is null) continue;

            if (activity.Meta.RootElement.TryGetProperty("assigneeId", out var element))
            {
                yield return element.GetString()!;
            }
        }
    }
}
