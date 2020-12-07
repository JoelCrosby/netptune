using System;
using System.Threading.Tasks;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Cache.Common;

namespace Netptune.Services.Cache
{
    public class UserCache : EntityCache<AppUser, string>, IUserCache
    {
        private readonly INetptuneUnitOfWork UnitOfWork;

        public UserCache(ICacheProvider cache, INetptuneUnitOfWork unitOfWork) : base(cache, TimeSpan.FromHours(1))
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
