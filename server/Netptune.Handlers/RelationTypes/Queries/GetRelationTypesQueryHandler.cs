using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Handlers.RelationTypes.Queries;

public sealed record GetRelationTypesQuery : IRequest<List<RelationTypeViewModel>?>;

public sealed class GetRelationTypesQueryHandler : IRequestHandler<GetRelationTypesQuery, List<RelationTypeViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetRelationTypesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<RelationTypeViewModel>?> Handle(GetRelationTypesQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        // Backfills workspaces that predate the feature, matching how statuses seed lazily on read.
        await UnitOfWork.RelationTypes.EnsureDefaultRelationTypes(workspaceId.Value, Identity.GetCurrentUserId(), cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return await UnitOfWork.RelationTypes.GetViewModelsForWorkspace(workspaceId.Value, cancellationToken);
    }
}
