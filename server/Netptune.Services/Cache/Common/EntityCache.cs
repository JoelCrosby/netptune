using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

using Netptune.Core.Cache;

namespace Netptune.Services.Cache.Common
{
    public abstract class EntityCache<TEntity, TKey> : IEntityCache<TEntity, TKey>
    {
        protected readonly ICacheProvider Cache;

        protected abstract Task<TEntity> GetEntity(TKey key);
        protected abstract string GetCacheKey(TKey key);

        private readonly TimeSpan TimeToLive;

        protected EntityCache(ICacheProvider cache, TimeSpan timeToLive)
        {
            Cache = cache;
            TimeToLive = timeToLive;
        }

        public Task<TEntity> Get(TKey key)
        {
            return GetOrCreateAsync(GetCacheKey(key), () => GetEntity(key));
        }

        public void Remove(TKey key)
        {
            Cache.Remove(GetCacheKey(key));
        }

        private async Task<TEntity> GetOrCreateAsync(string key, Func<Task<TEntity>> factory)
        {
            if (Cache.TryGetValue(key, out TEntity result))
            {
                return result;
            }

            var entity = await factory();

            await Cache.SetAsync(key, entity, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeToLive,
            });

            return entity;
        }
    }
}
