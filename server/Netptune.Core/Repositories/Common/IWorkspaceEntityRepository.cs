using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Repositories.Common
{
    /// <summary>
    /// Base Repository compatible with entity framework core and micro ORMs like Dapper
    /// Designed to do complex read queries with Dapper and write operations with EF Core
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public interface IWorkspaceEntityRepository<TEntity, TId> : IAuditableRepository<TEntity, TId>
        where TEntity : WorkspaceEntity<TId>
    {
        Task<List<TEntity>> GetAllInWorkspace(int workspaceId, bool includeDeleted = false, bool isReadonly = false);

        Task<List<TId>> GetAllIdsInWorkspace(int workspaceId, bool includeDeleted = false);

        Task DeleteAllInWorkspace(int workspaceId);
    }
}
