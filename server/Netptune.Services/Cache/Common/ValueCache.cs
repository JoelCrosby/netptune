using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

using Netptune.Core.Cache.Common;

namespace Netptune.Services.Cache.Common
{
    public abstract class ValueCache<TValue> : IValueCache<TValue>
    {
        protected readonly ICacheProvider Cache;

        private readonly TimeSpan TimeToLive;

        protected ValueCache(ICacheProvider cache, TimeSpan timeToLive)
        {
            Cache = cache;
            TimeToLive = timeToLive;
        }

        public virtual Task<TValue> Get(string key)
        {
            return Cache.GetValueAsync<TValue>(key);
        }

        public virtual void Remove(string key)
        {
            Cache.Remove(key);
        }

        public virtual Task Create(string key, TValue value)
        {
            return Cache.SetAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeToLive,
            });
        }
    }
}
