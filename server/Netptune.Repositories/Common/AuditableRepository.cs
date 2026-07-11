using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

public abstract class AuditableRepository<TContext, TEntity, TId> : Repository<TContext, TEntity, TId>, IAuditableRepository<TEntity, TId>
    where TContext : DbContext
    where TEntity : AuditableEntity<TId>
{
    protected AuditableRepository(TContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory)
    {
    }

    public async Task<TEntity?> Delete(TId id, AppUser user, CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(id, cancellationToken: cancellationToken);

        if (entity is null) return null;

        entity.IsDeleted = true;
        entity.DeletedByUserId = user.Id;

        return entity;
    }

    public Task<int> SoftDelete(TId id, string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(EqualsPredicate(id))
            .Where(entity => !entity.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.IsDeleted, true)
                .SetProperty(entity => entity.DeletedByUserId, userId)
                .SetProperty(entity => entity.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task<List<TId>> SoftDelete(IEnumerable<TId> ids, string userId, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return [];

        var affectedIds = await Entities
            .AsNoTracking()
            .Where(entity => idList.Contains(entity.Id) && !entity.IsDeleted)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (affectedIds.Count == 0) return affectedIds;

        await Entities
            .Where(entity => affectedIds.Contains(entity.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.IsDeleted, true)
                .SetProperty(entity => entity.DeletedByUserId, userId)
                .SetProperty(entity => entity.UpdatedAt, DateTime.UtcNow), cancellationToken);

        return affectedIds;
    }

    public async Task<List<TId>> Restore(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return [];

        var affectedIds = await Entities
            .AsNoTracking()
            .Where(entity => idList.Contains(entity.Id) && entity.IsDeleted)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (affectedIds.Count == 0) return affectedIds;

        await Entities
            .Where(entity => affectedIds.Contains(entity.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.IsDeleted, false)
                .SetProperty(entity => entity.DeletedByUserId, (string?)null)
                .SetProperty(entity => entity.UpdatedAt, DateTime.UtcNow), cancellationToken);

        return affectedIds;
    }

    public override Task<List<TEntity>> GetAllAsync(bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => !entity.IsDeleted)
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public override Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => !entity.IsDeleted && ids.Contains(entity.Id))
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }
}
