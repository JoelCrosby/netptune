using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Ordering;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.RelationTypes.Commands;

public sealed record MoveRelationTypeCommand(MoveRelationTypeRequest Request) : IRequest<ClientResponse>;

public sealed class MoveRelationTypeCommandHandler : IRequestHandler<MoveRelationTypeCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public MoveRelationTypeCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MoveRelationTypeCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return ClientResponse.NotFound;
        }

        var relationTypes = await UnitOfWork.RelationTypes.GetAllInWorkspace(workspaceId.Value, isReadonly: false, cancellationToken: cancellationToken);
        var siblings = relationTypes
            .Where(relationType => !relationType.IsDeleted)
            .OrderBy(relationType => relationType.SortOrder)
            .ThenBy(relationType => relationType.Id)
            .ToList();

        var moved = SortOrdering.Move(siblings, request.Request.Id, request.Request.Direction);

        if (moved is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = moved.Select(relationType => relationType.Id);
            options.EntityType = EntityType.RelationType;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }
}
