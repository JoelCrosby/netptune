using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.Hubs;
using Netptune.Core.Services;

namespace Netptune.Services
{
    public class UserConnectionService : IUserConnectionService
    {
        private readonly IMemoryCache Cache;
        private readonly IIdentityService Identity;

        public UserConnectionService(IMemoryCache cache, IIdentityService identity)
        {
            Cache = cache;
            Identity = identity;
        }

        public Task<UserConnection> Get(string connectionId)
        {
            return Task.FromResult(Cache.Get<UserConnection>(connectionId));
        }

        public async Task<UserConnection> Add(string connectionId)
        {
            if (Cache.TryGetValue<UserConnection>(connectionId, out var result))
            {
                return result;
            }

            var user = await Identity.GetCurrentUser();

            if (user is null) return null;

            return Cache.GetOrCreate(connectionId, _ => new UserConnection
            {
                ConnectId = connectionId,
                User = user,
                UserId = user.Id,
            });
        }

        public Task Remove(string connectionId)
        {
            Cache.Remove(connectionId);

            return Task.CompletedTask;
        }
    }
}
