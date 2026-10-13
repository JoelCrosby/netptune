using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageBodiesQuery(IReadOnlyCollection<int> Ids) : IRequest<ClientResponse<List<PageBodyViewModel>>>;

public sealed class GetPageBodiesQueryHandler : IRequestHandler<GetPageBodiesQuery, ClientResponse<List<PageBodyViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageBodiesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<PageBodyViewModel>>> Handle(GetPageBodiesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var bodies = await UnitOfWork.Pages.GetBodies(workspaceId, request.Ids, cancellationToken);

        return ClientResponse<List<PageBodyViewModel>>.Success(bodies);
    }
}
