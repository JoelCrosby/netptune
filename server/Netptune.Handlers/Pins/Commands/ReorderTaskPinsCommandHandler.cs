using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pins.Commands;

public sealed record TaskPinOrder(int Id, double SortOrder);

public sealed record ReorderTaskPinsRequest
{
    public required List<TaskPinOrder> Items { get; init; }
}

public sealed record ReorderTaskPinsCommand(ReorderTaskPinsRequest Request) : IRequest<ClientResponse>;

public sealed class ReorderTaskPinsCommandHandler : IRequestHandler<ReorderTaskPinsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public ReorderTaskPinsCommandHandler(
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

    public async ValueTask<ClientResponse> Handle(ReorderTaskPinsCommand request, CancellationToken cancellationToken)
    {
        var items = request.Request.Items;

        if (items.Count == 0)
        {
            return ClientResponse.Success;
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var ids = items.Select(item => item.Id).Distinct().ToList();
        var pins = await TaskPins.GetByIds(ids, workspaceId, cancellationToken);

        if (pins.Count != ids.Count)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.TryGetWorkspaceKey();
        var rights = await PinsPermissions.GetWriteRights(PermissionCache, userId, workspaceKey);
        var ownsEveryPersonalPin = pins.All(pin => pin.Scope != TaskPinScope.User || pin.CreatedByUserId == userId);
        var canWriteEveryScope = pins.All(pin => rights.For(pin.Scope));

        if (!ownsEveryPersonalPin || !canWriteEveryScope)
        {
            return ClientResponse.Forbidden;
        }

        var ordersById = items.ToDictionary(item => item.Id, item => item.SortOrder);

        foreach (var pin in pins)
        {
            pin.SortOrder = ordersById[pin.Id];
            pin.ModifiedByUserId = userId;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
