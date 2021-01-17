using System;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using Netptune.Core.Cache.Common;

using StackExchange.Redis;

namespace Netptune.Services.Cache.Redis
{
    public class RedisCache : ICacheProvider
    {
        private readonly ConnectionMultiplexer Redis;

        private IDatabase Db => Redis.GetDatabase();

        public RedisCache(IOptions<RedisCacheOptions> options)
        {
            if (options?.Value is null)
            {
                throw new Exception($"{nameof(RedisCache)} was instantiated without options provided");
            }

            Redis = ConnectionMultiplexer.Connect(options.Value.Connection);
        }

        public string GetString(string key)
        {
            return Db.StringGet(key);
        }

        public TValue GetValue<TValue>(string key)
        {
            if (key is null) return default;

            var json = GetString(key);

            if (json is null) return default;

            return JsonSerializer.Deserialize<TValue>(json);
        }

        public bool TryGetString(string key, out string value)
        {
            if (key is null)
            {
                value = null;
                return false;
            }

            if (GetString(key) is {} result)
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue<TValue>(string key, out TValue value)
        {
            if (key is null)
            {
                value = default;
                return false;
            }

            var result = Db.StringGet(key);

            if (result.IsNull)
            {
                value = default;
                return false;
            }

            value = JsonSerializer.Deserialize<TValue>(result);
            return true;
        }

        public async Task<(bool, TValue)> TryGetValueAsync<TValue>(string key)
        {
            if (key is null)
            {
                return (false, default);
            }

            var result = await Db.StringGetAsync(key);

            if (result.IsNull)
            {
                return (false, default);
            }

            var value = JsonSerializer.Deserialize<TValue>(result);
            return (true, value);
        }

        public async Task<string> GetStringAsync(string key)
        {
            if (key is null) return null;

            string result = await Db.StringGetAsync(key);

            return result;
        }

        public async Task<TValue> GetValueAsync<TValue>(string key)
        {
            if (key is null) return default;

            var json = await GetStringAsync(key);

            if (json is null) return default;
            return JsonSerializer.Deserialize<TValue>(json);
        }

        public TValue GetOrCreate<TValue>(string key, Func<TValue> factory, DistributedCacheEntryOptions options)
        {
            var value = GetValue<TValue>(key);

            if (value is { })
            {
                return value;
            }

            Set(key, factory.Invoke(), options);

            return value;
        }

        public async Task<TValue> GetOrCreateAsync<TValue>(string key, Func<TValue> factory, DistributedCacheEntryOptions options)
        {
            var value = await GetValueAsync<TValue>(key);

            if (value is { })
            {
                return value;
            }

            await SetAsync(key, factory.Invoke(), options);

            return value;
        }

        public void Remove(string key)
        {
            if (key is null) return;

            Db.KeyDelete(key);
        }

        public Task RemoveAsync(string key)
        {
            if (key is null) return Task.CompletedTask;

            return Db.KeyDeleteAsync(key);
        }

        public void Set(string key, string value, DistributedCacheEntryOptions options)
        {
            Db.StringSet(key, value, options.AbsoluteExpirationRelativeToNow);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            return Db.StringSetAsync(key, value, options.AbsoluteExpirationRelativeToNow);
        }

        public void Set<TValue>(string key, TValue value, DistributedCacheEntryOptions options)
        {
            if (key is null) return;

            var json = JsonSerializer.Serialize(value);
            Db.StringSet(key, json, options.AbsoluteExpirationRelativeToNow);
        }

        public Task SetAsync<TValue>(string key, TValue value, DistributedCacheEntryOptions options)
        {
            if (key is null) return Task.CompletedTask;

            var json = JsonSerializer.Serialize(value);
            return Db.StringSetAsync(key, json, options.AbsoluteExpirationRelativeToNow);
        }
    }
}
