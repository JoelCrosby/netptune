using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relations;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Handlers.RelationTypes.Commands;

public sealed record CreateRelationTypeCommand(CreateRelationTypeRequest Request) : IRequest<ClientResponse<RelationTypeViewModel>>;

public sealed class CreateRelationTypeCommandHandler : IRequestHandler<CreateRelationTypeCommand, ClientResponse<RelationTypeViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateRelationTypeCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<RelationTypeViewModel>> Handle(CreateRelationTypeCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse<RelationTypeViewModel>.NotFound;

        var name = request.Request.Name.Trim();
        var key = name.ToUrlSlug();

        if (string.IsNullOrWhiteSpace(key))
        {
            return ClientResponse<RelationTypeViewModel>.Failed("Relation type name must contain at least one valid character.");
        }

        if (await UnitOfWork.RelationTypes.KeyExists(workspaceId.Value, key, cancellationToken: cancellationToken))
        {
            return ClientResponse<RelationTypeViewModel>.Failed("Relation type name should be unique.");
        }

        var relationTypes = await UnitOfWork.RelationTypes.GetViewModelsForWorkspace(workspaceId.Value, cancellationToken);
        var sortOrder = relationTypes.Count == 0 ? 0 : relationTypes.Max(relationType => relationType.SortOrder) + 1;

        var relationType = new RelationType
        {
            WorkspaceId = workspaceId.Value,
            OwnerId = Identity.GetCurrentUserId(),
            Name = name,
            InverseName = RelationTypeRules.ResolveInverseName(request.Request.Category, name, request.Request.InverseName),
            Key = key,
            Description = request.Request.Description?.Trim(),
            Color = request.Request.Color?.Trim(),
            Category = request.Request.Category,
            SortOrder = sortOrder,
        };

        await UnitOfWork.RelationTypes.AddAsync(relationType, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.RelationTypes.GetViewModel(relationType.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = relationType.Id;
            options.EntityType = EntityType.RelationType;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<RelationTypeViewModel>.Success(result!);
    }
}
