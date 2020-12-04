using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Cache.Common;

namespace Netptune.Services.Cache
{
    public class UserCache : EntityCache<AppUser, string>, IUserCache
    {
        private readonly INetptuneUnitOfWork UnitOfWork;

        public UserCache(IMemoryCache cache, INetptuneUnitOfWork unitOfWork) : base(cache)
        {
            UnitOfWork = unitOfWork;
        }

        protected override Task<AppUser> GetEntity(string key)
        {
            return UnitOfWork.Users.GetAsync(key);
        }

        protected override string GetCacheKey(string key)
        {
            return $"user:{key}";
        }
    }
}
