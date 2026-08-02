using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceAiCredentialRepository : IRepository<WorkspaceAiCredential, Guid>
{
    Task<List<WorkspaceAiCredential>> GetForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<WorkspaceAiCredential?> GetForProvider(int workspaceId, AiProvider provider, CancellationToken cancellationToken = default);

    Task<WorkspaceAiCredential?> GetOwned(Guid credentialId, int workspaceId, CancellationToken cancellationToken = default);
}
