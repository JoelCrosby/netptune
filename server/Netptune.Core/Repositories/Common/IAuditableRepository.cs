using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Repositories.Common;

public interface IAuditableRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AuditableEntity<TId>
{
    Task<TEntity?> Delete(TId id, AppUser user, CancellationToken cancellationToken = default);

    Task<int> SoftDelete(TId id, string userId, CancellationToken cancellationToken = default);

    Task<List<TId>> SoftDelete(IEnumerable<TId> ids, string userId, CancellationToken cancellationToken = default);

    Task<List<TId>> Restore(IEnumerable<TId> ids, CancellationToken cancellationToken = default);
}
