using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.RelationTypes.Commands;

public sealed record ReorderRelationTypesCommand(ReorderRelationTypesRequest Request) : IRequest<ClientResponse>;

public sealed class ReorderRelationTypesCommandHandler : IRequestHandler<ReorderRelationTypesCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public ReorderRelationTypesCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(ReorderRelationTypesCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var relationTypes = await UnitOfWork.RelationTypes.GetAllInWorkspace(workspaceId.Value, isReadonly: false, cancellationToken: cancellationToken);
        var relationTypeMap = relationTypes.ToDictionary(relationType => relationType.Id);

        foreach (var (relationTypeId, index) in request.Request.RelationTypeIds.Select((relationTypeId, index) => (relationTypeId, index)))
        {
            if (relationTypeMap.TryGetValue(relationTypeId, out var relationType))
            {
                relationType.SortOrder = index;
            }
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = request.Request.RelationTypeIds.Where(relationTypeMap.ContainsKey);
            options.EntityType = EntityType.RelationType;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }
}
