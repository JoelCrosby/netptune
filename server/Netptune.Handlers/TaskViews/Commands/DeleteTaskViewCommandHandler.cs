using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Commands;

public sealed record DeleteTaskViewCommand(string Slug) : IRequest<ClientResponse>;

public sealed class DeleteTaskViewCommandHandler : IRequestHandler<DeleteTaskViewCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskViewRepository TaskViews;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public DeleteTaskViewCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ITaskViewRepository taskViews,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        TaskViews = taskViews;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTaskViewCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var view = await TaskViews.GetBySlug(request.Slug, workspaceId, cancellationToken: cancellationToken);

        if (view is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isOwn = view.CreatedByUserId == userId;

        if (!isOwn)
        {
            var workspaceKey = Identity.TryGetWorkspaceKey();
            var canManageShared = await TaskViewPermissions.CanManageShared(PermissionCache, userId, workspaceKey);

            if (!canManageShared)
            {
                return ClientResponse.Forbidden;
            }
        }

        view.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
