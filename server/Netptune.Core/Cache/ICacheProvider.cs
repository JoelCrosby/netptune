using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

namespace Netptune.Core.Cache
{
    public interface ICacheProvider
    {
        string GetString(string key);

        TValue GetValue<TValue>(string key);

        Task<string> GetStringAsync(string key);

        Task<TValue> GetValueAsync<TValue>(string key);

        void Remove(string key);

        Task RemoveAsync(string key);

        void Set(string key, byte[] value, DistributedCacheEntryOptions options);

        void Set<TValue>(string key, TValue value, DistributedCacheEntryOptions options);

        Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetAsync<TValue>(string key, TValue value, DistributedCacheEntryOptions options);

        bool TryGetString(string key, out string value);

        bool TryGetValue<TValue>(string key, out TValue value);

        Task<(bool, TValue)> TryGetValueAsync<TValue>(string key);
    }
}
