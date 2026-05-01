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
