using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pins;

namespace Netptune.Handlers.Pins.Queries;

public sealed record GetBoardPinsQuery(int BoardId) : IRequest<ClientResponse<List<PinnedTaskViewModel>>>;

public sealed class GetBoardPinsQueryHandler : IRequestHandler<GetBoardPinsQuery, ClientResponse<List<PinnedTaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetBoardPinsQueryHandler(
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

    public async ValueTask<ClientResponse<List<PinnedTaskViewModel>>> Handle(GetBoardPinsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var board = await UnitOfWork.Boards.GetInWorkspace(request.BoardId, workspaceId, true, cancellationToken);

        if (board is null || board.IsDeleted)
        {
            return ClientResponse<List<PinnedTaskViewModel>>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.TryGetWorkspaceKey();
        var rights = await PinsPermissions.GetWriteRights(PermissionCache, userId, workspaceKey);
        var pins = await TaskPins.GetForBoard(board.Id, board.ProjectId, workspaceId, userId, cancellationToken);
        var scope = new PinProjectionScope
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Rights = rights,
        };
        var pinnedTasks = await PinnedTaskProjection.Build(UnitOfWork, pins, scope, cancellationToken);

        return ClientResponse<List<PinnedTaskViewModel>>.Success(pinnedTasks);
    }
}
