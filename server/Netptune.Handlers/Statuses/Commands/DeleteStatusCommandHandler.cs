using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record DeleteStatusCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteStatusCommandHandler : IRequestHandler<DeleteStatusCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteStatusCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var status = await UnitOfWork.Statuses.GetInWorkspace(request.Id, workspaceId.Value, cancellationToken: cancellationToken);

        if (status is null) return ClientResponse.NotFound;
        if (status.IsSystem) return ClientResponse.Failed("System statuses cannot be deleted.");

        var isInUse = await UnitOfWork.Statuses.IsInUse(status.Id, cancellationToken);
        if (isInUse) return ClientResponse.Failed("Status is in use and cannot be deleted.");

        status.Delete(Identity.GetCurrentUserId());
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = status.Id;
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
