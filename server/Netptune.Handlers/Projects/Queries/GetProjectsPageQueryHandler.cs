using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Handlers.Projects.Queries;

public sealed record GetProjectsPageQuery(ProjectFilter Filter) : IRequest<ClientResponse<PagedResponse<ProjectViewModel>>>;

public sealed class GetProjectsPageQueryHandler : IRequestHandler<GetProjectsPageQuery, ClientResponse<PagedResponse<ProjectViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetProjectsPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<ProjectViewModel>>> Handle(GetProjectsPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var page = await UnitOfWork.Projects.GetProjectsPage(workspaceKey, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<ProjectViewModel>>.Success(page);
    }
}
