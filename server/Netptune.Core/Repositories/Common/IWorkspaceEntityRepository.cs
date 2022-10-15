using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Repositories.Common;

public interface IWorkspaceEntityRepository<TEntity, TId> : IAuditableRepository<TEntity, TId>
    where TEntity : WorkspaceEntity<TId>
{
    Task<List<TEntity>> GetAllInWorkspace(int workspaceId, bool includeDeleted = false, bool isReadonly = false);

    Task<List<TId>> GetAllIdsInWorkspace(int workspaceId, bool includeDeleted = false);

    Task DeleteAllInWorkspace(int workspaceId);
}
