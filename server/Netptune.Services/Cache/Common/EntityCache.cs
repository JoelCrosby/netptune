using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Netptune.Core.Cache.Common;

namespace Netptune.Services.Cache.Common
{
    public abstract class EntityCache<TEntity, TKey> : IEntityCache<TEntity, TKey>
    {
        protected readonly ICacheProvider Cache;

        protected abstract Task<TEntity> GetEntity(TKey key);
        protected abstract string GetCacheKey(TKey key);

        private readonly TimeSpan TimeToLive;
        private readonly ILogger<EntityCache<TEntity, TKey>> Logger;

        protected EntityCache(ICacheProvider cache, TimeSpan timeToLive, ILogger<EntityCache<TEntity, TKey>> logger)
        {
            Cache = cache;
            TimeToLive = timeToLive;
            Logger = logger;
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
            var watch = Stopwatch.StartNew();

            var (hit, value) = await Cache.TryGetValueAsync<TEntity>(key);

            if (hit)
            {
                Logger.LogInformation($"[REDIS] [GetOrCreateAsync] key: {key} responded in {watch.ElapsedMilliseconds}ms");

                return value;
            }

            var entity = await factory();

            await Cache.SetAsync(key, entity, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeToLive,
            });

            Logger.LogInformation($"[REDIS] [GetOrCreateAsync] key: {key} responded in {watch.ElapsedMilliseconds}ms");

            return entity;
        }
    }
}
