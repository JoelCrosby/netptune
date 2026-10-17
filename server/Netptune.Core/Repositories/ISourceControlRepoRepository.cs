using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ISourceControlRepoRepository : IRepository<SourceControlRepo, int>
{
    Task<List<SourceControlRepo>> GetForConnection(int connectionId, CancellationToken cancellationToken = default);

    Task<List<SourceControlRepo>> GetLinkedForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<SourceControlRepo?> GetOwned(int repoId, int workspaceId, CancellationToken cancellationToken = default);

    Task<SourceControlRepo?> GetByExternalId(int connectionId, string externalId, CancellationToken cancellationToken = default);
}
