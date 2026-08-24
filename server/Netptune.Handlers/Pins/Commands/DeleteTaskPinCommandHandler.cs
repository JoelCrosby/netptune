using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pins.Commands;

public sealed record DeleteTaskPinCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteTaskPinCommandHandler : IRequestHandler<DeleteTaskPinCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public DeleteTaskPinCommandHandler(
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

    public async ValueTask<ClientResponse> Handle(DeleteTaskPinCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var pin = await TaskPins.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (pin is null || pin.IsDeleted)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isSomeoneElsesPersonalPin = pin.Scope == TaskPinScope.User && pin.CreatedByUserId != userId;

        if (isSomeoneElsesPersonalPin)
        {
            return ClientResponse.NotFound;
        }

        var workspaceKey = Identity.TryGetWorkspaceKey();
        var canWrite = await PinsPermissions.CanWrite(PermissionCache, userId, workspaceKey, pin.Scope);

        if (!canWrite)
        {
            return ClientResponse.Forbidden;
        }

        pin.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
