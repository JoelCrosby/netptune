using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.RelationTypes.Commands;

public sealed record DeleteRelationTypeCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteRelationTypeCommandHandler : IRequestHandler<DeleteRelationTypeCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteRelationTypeCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteRelationTypeCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.Id, workspaceId.Value, cancellationToken: cancellationToken);

        if (relationType is null) return ClientResponse.NotFound;
        if (relationType.IsSystem) return ClientResponse.Failed("System relation types cannot be deleted.");

        var isInUse = await UnitOfWork.RelationTypes.IsInUse(relationType.Id, cancellationToken);
        if (isInUse) return ClientResponse.Failed("Relation type is in use and cannot be deleted.");

        relationType.Delete(Identity.GetCurrentUserId());
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = relationType.Id;
            options.EntityType = EntityType.RelationType;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
