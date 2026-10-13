using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Queries;

public sealed record GetSprintsPageQuery(SprintPageFilter Filter) : IRequest<ClientResponse<PagedResponse<SprintViewModel>>>;

public sealed class GetSprintsPageQueryHandler : IRequestHandler<GetSprintsPageQuery, ClientResponse<PagedResponse<SprintViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintsPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<SprintViewModel>>> Handle(GetSprintsPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var page = await UnitOfWork.Sprints.GetSprintsPage(workspaceKey, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<SprintViewModel>>.Success(page);
    }
}
