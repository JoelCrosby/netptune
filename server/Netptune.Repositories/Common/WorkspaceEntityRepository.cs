using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

public abstract class WorkspaceEntityRepository<TContext, TEntity, TId>
    : AuditableRepository<TContext, TEntity, TId>, IWorkspaceEntityRepository<TEntity, TId>
    where TContext : DbContext
    where TEntity : WorkspaceEntity<TId>
{
    protected WorkspaceEntityRepository(TContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<TEntity>> GetAllInWorkspace(int workspaceId, bool includeDeleted = false, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => entity.WorkspaceId == workspaceId && (includeDeleted || !entity.IsDeleted))
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public Task<List<TId>> GetAllIdsInWorkspace(int workspaceId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => entity.WorkspaceId == workspaceId && (includeDeleted || !entity.IsDeleted))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAllInWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        var entityIds = await GetAllIdsInWorkspace(workspaceId, true, cancellationToken);

        await DeletePermanent(entityIds, cancellationToken);
    }
}
