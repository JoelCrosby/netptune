using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Handlers.RelationTypes.Queries;

public sealed record GetRelationTypesPageQuery(RelationTypeFilter Filter) : IRequest<ClientResponse<PagedResponse<RelationTypeViewModel>>>;

public sealed class GetRelationTypesPageQueryHandler : IRequestHandler<GetRelationTypesPageQuery, ClientResponse<PagedResponse<RelationTypeViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetRelationTypesPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<RelationTypeViewModel>>> Handle(GetRelationTypesPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return ClientResponse<PagedResponse<RelationTypeViewModel>>.NotFound;
        }

        var page = await UnitOfWork.RelationTypes.GetPageForWorkspace(workspaceId.Value, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<RelationTypeViewModel>>.Success(page);
    }
}
