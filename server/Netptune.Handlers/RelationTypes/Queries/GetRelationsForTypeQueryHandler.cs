using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Handlers.RelationTypes.Queries;

public sealed record GetRelationsForTypeQuery(int Id, PageRequest Page) : IRequest<PagedResponse<RelationTypeRelationViewModel>?>;

public sealed class GetRelationsForTypeQueryHandler : IRequestHandler<GetRelationsForTypeQuery, PagedResponse<RelationTypeRelationViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetRelationsForTypeQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<PagedResponse<RelationTypeRelationViewModel>?> Handle(GetRelationsForTypeQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.Id, workspaceId.Value, true, cancellationToken);

        if (relationType is null) return null;

        var relations = await UnitOfWork.ProjectTaskRelations.GetRelationsForType(
            relationType.Id,
            workspaceId.Value,
            request.Page,
            cancellationToken);

        return relations;
    }
}
