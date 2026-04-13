using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services;

public class PublicWorkspaceService : IPublicWorkspaceService
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public PublicWorkspaceService(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<Workspace?> GetPublicWorkspace(string slug)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(slug, isReadonly: true);

        if (workspace is null || !workspace.IsPublic) return null;

        return workspace;
    }
}
