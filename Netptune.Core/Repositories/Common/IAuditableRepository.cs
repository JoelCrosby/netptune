using Netptune.Models;
using Netptune.Models.BaseEntities;

using System.Threading.Tasks;

namespace Netptune.Core.Repositories.Common
{
    /// <summary>
    /// Base Repository compatible with entity framework core and micro ORMs like Dapper
    /// Designed to do complex read queries with Dapper and write operations with EF Core
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public interface IAuditableRepository<TEntity, in TId> : IRepository<TEntity, TId>
        where TEntity : AuditableEntity<TId>
    {
        /// <summary>
        /// Deletes the entity and updates meta data.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<TEntity> Delete(TId id, AppUser user);

        /// <summary>
        /// Permanently Deletes the entity.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity> DeletePermanent(TId id);
    }
}
