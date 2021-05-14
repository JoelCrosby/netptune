using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

using Netptune.Core.Cache.Common;
using Netptune.Core.Hubs;
using Netptune.Core.Services;

namespace Netptune.Services
{
    public class UserConnectionService : IUserConnectionService
    {
        private readonly ICacheProvider Cache;
        private readonly IIdentityService Identity;

        public UserConnectionService(ICacheProvider cache, IIdentityService identity)
        {
            Cache = cache;
            Identity = identity;
        }

        public Task<UserConnection> Get(string connectionId)
        {
            return Cache.GetValueAsync<UserConnection>(connectionId);
        }

        public async Task<UserConnection> Add(string connectionId)
        {
            if (Cache.TryGetValue<UserConnection>(connectionId, out var result))
            {
                return result;
            }

            var user = await Identity.GetCurrentUser();

            if (user is null) return null;

            return Cache.GetOrCreate(connectionId, () => new UserConnection
            {
                ConnectId = connectionId,
                User = user,
                UserId = user.Id,
            }, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            });
        }

        public Task Remove(string connectionId)
        {
            Cache.Remove(connectionId);

            return Task.CompletedTask;
        }
    }
}
