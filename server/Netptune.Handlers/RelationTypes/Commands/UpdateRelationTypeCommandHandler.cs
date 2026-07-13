using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Relations;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Handlers.RelationTypes.Commands;

public sealed record UpdateRelationTypeCommand(UpdateRelationTypeRequest Request) : IRequest<ClientResponse<RelationTypeViewModel>>;

public sealed class UpdateRelationTypeCommandHandler : IRequestHandler<UpdateRelationTypeCommand, ClientResponse<RelationTypeViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateRelationTypeCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<RelationTypeViewModel>> Handle(UpdateRelationTypeCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse<RelationTypeViewModel>.NotFound;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.Request.Id, workspaceId.Value, cancellationToken: cancellationToken);

        if (relationType is null) return ClientResponse<RelationTypeViewModel>.NotFound;

        var name = request.Request.Name.Trim();
        var key = name.ToUrlSlug();

        if (string.IsNullOrWhiteSpace(key))
        {
            return ClientResponse<RelationTypeViewModel>.Failed("Relation type name must contain at least one valid character.");
        }

        if (await UnitOfWork.RelationTypes.KeyExists(workspaceId.Value, key, relationType.Id, cancellationToken))
        {
            return ClientResponse<RelationTypeViewModel>.Failed("Relation type name should be unique.");
        }

        relationType.Name = name;
        relationType.Key = key;

        // Category is immutable, so the symmetry of this type cannot change here — the inverse is
        // resolved against the category the type already has.
        relationType.InverseName = RelationTypeRules.ResolveInverseName(relationType.Category, name, request.Request.InverseName);
        relationType.Description = request.Request.Description?.Trim();
        relationType.Color = request.Request.Color?.Trim();

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.RelationTypes.GetViewModel(relationType.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = relationType.Id;
            options.EntityType = EntityType.RelationType;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse<RelationTypeViewModel>.Success(result!);
    }
}
