using Mediator;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Queries;

public sealed record GetProjectsQuery : IRequest<List<ProjectViewModel>>;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, List<ProjectViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetProjectsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public ValueTask<List<ProjectViewModel>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        return new ValueTask<List<ProjectViewModel>>(UnitOfWork.Projects.GetProjects(workspaceKey));
    }
}
