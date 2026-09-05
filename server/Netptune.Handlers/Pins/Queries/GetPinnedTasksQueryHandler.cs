using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pins;

namespace Netptune.Handlers.Pins.Queries;

public sealed record GetPinnedTasksQuery : IRequest<List<PinnedTaskViewModel>>;

public sealed class GetPinnedTasksQueryHandler : IRequestHandler<GetPinnedTasksQuery, List<PinnedTaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetPinnedTasksQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskPinRepository taskPins,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        TaskPins = taskPins;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<List<PinnedTaskViewModel>> Handle(GetPinnedTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        var userId = Identity.TryGetCurrentUserId();
        var workspaceKey = Identity.TryGetWorkspaceKey();
        var rights = await PinsPermissions.GetWriteRights(PermissionCache, userId, workspaceKey);
        var pins = await TaskPins.GetVisibleInWorkspace(workspaceId, userId, cancellationToken);
        var scope = new PinProjectionScope
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Rights = rights,
        };

        return await PinnedTaskProjection.Build(UnitOfWork, pins, scope, cancellationToken);
    }
}
