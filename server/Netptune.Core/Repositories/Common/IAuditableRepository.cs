using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Repositories.Common;

public interface IAuditableRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AuditableEntity<TId>
{
    Task<TEntity?> Delete(TId id, AppUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes the entity with the given id by setting <c>IsDeleted</c>,
    /// stamping the deleting user and update timestamp via a single set-based
    /// UPDATE (no entity is loaded). Returns the number of rows affected.
    /// </summary>
    Task<int> SoftDelete(TId id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes the entities with the given ids in a single set-based UPDATE
    /// (no entities are loaded). Returns the ids that were actually deleted
    /// (those that existed and were not already deleted).
    /// </summary>
    Task<List<TId>> SoftDelete(IEnumerable<TId> ids, string userId, CancellationToken cancellationToken = default);
}
