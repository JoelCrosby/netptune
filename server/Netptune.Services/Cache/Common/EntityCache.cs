using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.Cache;

namespace Netptune.Services.Cache.Common
{
    public abstract class EntityCache<TEntity, TKey> : IEntityCache<TEntity, TKey>
    {
        protected readonly IMemoryCache Cache;

        protected abstract Task<TEntity> GetEntity(TKey key);
        protected abstract string GetCacheKey(TKey key);

        protected EntityCache(IMemoryCache cache)
        {
            Cache = cache;
        }

        public Task<TEntity> Get(TKey key)
        {
            return Cache.GetOrCreateAsync(GetCacheKey(key), entry => GetEntity(key));
        }

        public void Remove(TKey key)
        {
            Cache.Remove(GetCacheKey(key));
        }
    }
}
