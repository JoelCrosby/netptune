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

    public virtual Task<TEntity?> GetInWorkspace(TId id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(isReadonly)
            .Where(entity => entity.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(EqualsPredicate(id), cancellationToken);
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

    public Task<List<TId>> GetExistingIds(
        IReadOnlyCollection<TId> ids,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Task.FromResult(new List<TId>());
        }

        return Entities
            .AsNoTracking()
            .Where(entity => ids.Contains(entity.Id) && entity.WorkspaceId == workspaceId && !entity.IsDeleted)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
    }
}
