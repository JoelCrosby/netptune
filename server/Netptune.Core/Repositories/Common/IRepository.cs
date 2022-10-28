using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories.Common;

public interface IRepository<TEntity, in TId> where TEntity : class
{
    Task<TEntity?> GetAsync(TId id, bool isReadonly = false);

    Task<List<TEntity>> GetAllAsync(bool isReadonly = false);

    Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false);

    Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery, bool isReadonly = false);

    Task<TEntity> AddAsync(TEntity entity);

    Task AddRangeAsync(IEnumerable<TEntity> entities);

    Task<TEntity?> DeletePermanent(TId id);

    Task DeletePermanent(IEnumerable<TId> idList);

    Task DeletePermanent(IEnumerable<TEntity> entities);
}
