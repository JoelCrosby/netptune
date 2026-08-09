using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceSearchCredentialRepository : IRepository<WorkspaceSearchCredential, Guid>
{
    Task<WorkspaceSearchCredential?> GetForWorkspace(int workspaceId, CancellationToken cancellationToken = default);
}
