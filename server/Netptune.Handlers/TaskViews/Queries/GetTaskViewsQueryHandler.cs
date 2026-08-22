using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Queries;

public sealed record GetTaskViewsQuery : IRequest<List<TaskViewViewModel>>;

public sealed class GetTaskViewsQueryHandler : IRequestHandler<GetTaskViewsQuery, List<TaskViewViewModel>>
{
    private readonly ITaskViewRepository TaskViews;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetTaskViewsQueryHandler(IIdentityService identity, ITaskViewRepository taskViews, IWorkspacePermissionCache permissionCache)
    {
        Identity = identity;
        TaskViews = taskViews;
        PermissionCache = permissionCache;
    }

    public async ValueTask<List<TaskViewViewModel>> Handle(GetTaskViewsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.TryGetWorkspaceKey();
        var canManageShared = await TaskViewPermissions.CanManageShared(PermissionCache, userId, workspaceKey);
        var views = await TaskViews.GetVisibleInWorkspace(workspaceId, userId, cancellationToken);

        return views.Select(view => TaskViewMapper.ToViewModel(view, userId, canManageShared)).ToList();
    }
}
