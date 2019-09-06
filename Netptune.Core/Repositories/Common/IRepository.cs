using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories.Common
{
    /// <summary>
    /// Base Repository compatible with entity framework core and micro ORMs like Dapper
    /// Designed to do complex read queries with Dapper and write operations with EF Core
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public interface IRepository<TEntity, in TId> where TEntity : class
    {
        /// <summary>
        /// Basic get query using entity id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Entity of the defined type</returns>
        TEntity Get(TId id);

        /// <summary>
        /// Basic get query using entity id async
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Entity of the defined type</returns>
        Task<TEntity> GetAsync(TId id);

        /// <summary>
        /// Return all Entities
        /// </summary>
        /// <returns>List of Entities</returns>
        IList<TEntity> GetAll();

        /// <summary>
        /// Return all Entities async
        /// </summary>
        /// <returns>List of Entities</returns>
        Task<IList<TEntity>> GetAllAsync();

        /// <summary>
        /// Return all Entities Within the given page query.
        /// </summary>
        /// <returns>List of Entities</returns>
        IPagedResult<TEntity> GetPagedResults(IPageQuery pageQuery);

        /// <summary>
        /// Return all Entities Within the given page query async.
        /// </summary>
        /// <returns>List of Entities</returns>
        Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery);
    }
}
