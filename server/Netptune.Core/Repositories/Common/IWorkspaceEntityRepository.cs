using Netptune.Core.BaseEntities;

namespace Netptune.Core.Repositories.Common;

public interface IWorkspaceEntityRepository<TEntity, TId> : IAuditableRepository<TEntity, TId>
    where TEntity : WorkspaceEntity<TId>
{
    Task<List<TEntity>> GetAllInWorkspace(int workspaceId, bool includeDeleted = false, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<TId>> GetAllIdsInWorkspace(int workspaceId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task DeleteAllInWorkspace(int workspaceId, CancellationToken cancellationToken = default);
}
