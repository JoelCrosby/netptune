using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common
{
    /// <summary>
    /// Base Repository compatible with entity framework core and micro ORMs like Dapper
    /// Designed to do complex read queries with Dapper and write operations with EF Core
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public abstract class WorkspaceEntityRepository<TContext, TEntity, TId>
        : AuditableRepository<TContext, TEntity, TId>, IWorkspaceEntityRepository<TEntity, TId>
        where TContext : DbContext
        where TEntity : WorkspaceEntity<TId>
    {
        protected WorkspaceEntityRepository(TContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<List<TEntity>> GetAllInWorkspace(int workspaceId, bool includeDeleted = false, bool isReadonly = false)
        {
            return Entities
                .Where(entity => entity.WorkspaceId == workspaceId && (includeDeleted || !entity.IsDeleted))
                .ToReadonlyListAsync(isReadonly);
        }

        public Task<List<TId>> GetAllIdsInWorkspace(int workspaceId, bool includeDeleted = false)
        {
            return Entities
                .Where(entity => entity.WorkspaceId == workspaceId && (includeDeleted || !entity.IsDeleted))
                .Select(entity => entity.Id)
                .ToListAsync();
        }

        public async Task DeleteAllInWorkspace(int workspaceId)
        {
            var entityIds = await GetAllIdsInWorkspace(workspaceId, true);

            await DeletePermanent(entityIds);
        }
    }
}
