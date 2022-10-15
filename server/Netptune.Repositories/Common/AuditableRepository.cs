using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task<TEntity?> Delete(TId id, AppUser user)
    {
        var entity = await GetAsync(id);

        if (entity is null) return null;

        entity.IsDeleted = true;
        entity.DeletedByUserId = user.Id;

        return entity;
    }
    public override Task<List<TEntity>> GetAllAsync(bool isReadonly = false)
    {
        return Entities
            .Where(entity => !entity.IsDeleted)
            .ToReadonlyListAsync(isReadonly);
    }

    public override Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false)
    {
        return Entities
            .Where(entity => !entity.IsDeleted && ids.Contains(entity.Id))
            .ToReadonlyListAsync(isReadonly);
    }
}
