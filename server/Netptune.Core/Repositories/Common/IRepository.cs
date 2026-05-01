namespace Netptune.Core.Repositories.Common;

public interface IRepository<TEntity, in TId> where TEntity : class
{
    Task<TEntity?> GetAsync(TId id, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity?> DeletePermanent(TId id, CancellationToken cancellationToken = default);

    Task DeletePermanent(IEnumerable<TId> idList, CancellationToken cancellationToken = default);

    Task DeletePermanent(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}
