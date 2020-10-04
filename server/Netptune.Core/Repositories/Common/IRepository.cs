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
        /// <param name="isReadonly"></param>
        /// <returns>Entity of the defined type</returns>
        TEntity Get(TId id, bool isReadonly = false);

        /// <summary>
        /// Basic get query using entity id async
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isReadonly"></param>
        /// <returns>Entity of the defined type</returns>
        Task<TEntity> GetAsync(TId id, bool isReadonly = false);

        /// <summary>
        /// Return all Entities
        /// </summary>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        List<TEntity> GetAll(bool isReadonly = false);

        /// <summary>
        /// Return all Entities async
        /// </summary>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        Task<List<TEntity>> GetAllAsync(bool isReadonly = false);

        /// <summary>
        /// Return all Entities from given IDs
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        List<TEntity> GetAllById(IEnumerable<TId> ids, bool isReadonly = false);

        /// <summary>
        /// Return all Entities from given IDs async
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false);

        /// <summary>
        /// Return all Entities Within the given page query.
        /// </summary>
        /// <param name="pageQuery"></param>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        IPagedResult<TEntity> GetPagedResults(IPageQuery pageQuery, bool isReadonly = false);

        /// <summary>
        /// Return all Entities Within the given page query async.
        /// </summary>
        /// <param name="pageQuery"></param>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery, bool isReadonly = false);

        /// <summary>
        /// Add Entity to store
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Entity of the defined type</returns>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Add Entity to store async
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Entity of the defined type</returns>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Add range of Entities to store
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>Entity of the defined type</returns>
        void AddRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// Add range of Entities to store async
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>Entity of the defined type</returns>
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Permanently Deletes the entity.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity> DeletePermanent(TId id);

        /// <summary>
        /// Permanently Deletes the given entities.
        /// </summary>
        /// <param name="idList"></param>
        /// <returns></returns>
        Task DeletePermanent(IEnumerable<TId> idList);

        /// <summary>
        /// Permanently Deletes the given entities.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task DeletePermanent(IEnumerable<TEntity> entities);
    }
}
