using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Ordering;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record MoveStatusCommand(MoveStatusRequest Request) : IRequest<ClientResponse>;

public sealed class MoveStatusCommandHandler : IRequestHandler<MoveStatusCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public MoveStatusCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MoveStatusCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return ClientResponse.NotFound;
        }

        var statuses = await UnitOfWork.Statuses.GetAllInWorkspace(workspaceId.Value, isReadonly: false, cancellationToken: cancellationToken);
        var siblings = statuses
            .Where(status => status.EntityType == EntityType.Task && !status.IsDeleted)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .ToList();

        var moved = SortOrdering.Move(siblings, request.Request.Id, request.Request.Direction);

        if (moved is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = moved.Select(status => status.Id);
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }
}
