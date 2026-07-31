using Microsoft.Extensions.Caching.Distributed;

using Netptune.Core.Cache.Common;

namespace Netptune.Cache.Common;

public abstract class EntityCache<TEntity, TKey> : IEntityCache<TEntity, TKey>
{
    protected readonly ICacheProvider Cache;

    protected abstract Task<TEntity?> GetEntity(TKey key);
    protected abstract string GetCacheKey(TKey key);

    // A missing entity is a state that turns into a present one - a workspace gets renamed,
    // a member gets invited, a permission gets granted. Storing the absence would hold the
    // stale answer for the whole time to live, so only resolved entities are cached.
    protected virtual bool ShouldCache(TEntity? entity)
    {
        return entity is not null;
    }

    private readonly TimeSpan TimeToLive;

    protected EntityCache(ICacheProvider cache, TimeSpan timeToLive)
    {
        Cache = cache;
        TimeToLive = timeToLive;
    }

    public Task<TEntity?> Get(TKey key)
    {
        return GetOrCreateAsync(GetCacheKey(key), () => GetEntity(key));
    }

    public void Remove(TKey key)
    {
        Cache.Remove(GetCacheKey(key));
    }

    private async Task<TEntity?> GetOrCreateAsync(string key, Func<Task<TEntity?>> factory)
    {
        var (hit, value) = await Cache.TryGetValueAsync<TEntity>(key);

        if (hit)
        {
            return value;
        }

        var entity = await factory();

        if (!ShouldCache(entity))
        {
            return entity;
        }

        await Cache.SetAsync(key, entity, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeToLive,
        });

        return entity;
    }
}
