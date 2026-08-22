using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Queries;

public sealed record GetTaskViewQuery(string Slug) : IRequest<ClientResponse<TaskViewViewModel>>;

public sealed class GetTaskViewQueryHandler : IRequestHandler<GetTaskViewQuery, ClientResponse<TaskViewViewModel>>
{
    private readonly ITaskViewRepository TaskViews;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetTaskViewQueryHandler(IIdentityService identity, ITaskViewRepository taskViews, IWorkspacePermissionCache permissionCache)
    {
        Identity = identity;
        TaskViews = taskViews;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<TaskViewViewModel>> Handle(GetTaskViewQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var view = await TaskViews.GetBySlug(request.Slug, workspaceId, true, cancellationToken);

        if (view is null)
        {
            return ClientResponse<TaskViewViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isVisible = view.IsShared || view.CreatedByUserId == userId;

        if (!isVisible)
        {
            return ClientResponse<TaskViewViewModel>.NotFound;
        }

        var workspaceKey = Identity.TryGetWorkspaceKey();
        var canManageShared = await TaskViewPermissions.CanManageShared(PermissionCache, userId, workspaceKey);
        var viewModel = TaskViewMapper.ToViewModel(view, userId, canManageShared);

        return ClientResponse<TaskViewViewModel>.Success(viewModel);
    }
}
