using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, ProjectViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetProjectQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ProjectViewModel?> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (workspaceId is null) return null;

        return await UnitOfWork.Projects.GetProjectViewModel(request.Key, workspaceId.Value);
    }
}
