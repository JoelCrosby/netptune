using System;
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

        private readonly TimeSpan TimeToLive;

        protected EntityCache(IMemoryCache cache, TimeSpan timeToLive)
        {
            Cache = cache;
            TimeToLive = timeToLive;
        }

        public Task<TEntity> Get(TKey key)
        {
            return GetOrCreateAsync(Cache, GetCacheKey(key), entry => GetEntity(key));
        }

        public void Remove(TKey key)
        {
            Cache.Remove(GetCacheKey(key));
        }

        private async Task<TItem> GetOrCreateAsync<TItem>(IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            if (cache.TryGetValue(key, out TItem result))
            {
                return result;
            }

            using var entry = cache.CreateEntry(key);

            result = await factory(entry).ConfigureAwait(false);
            entry.Value = result;
            entry.AbsoluteExpirationRelativeToNow = TimeToLive;

            return result;
        }
    }
}
