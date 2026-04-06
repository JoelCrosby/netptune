using Microsoft.Extensions.Logging;

using Netptune.Cache.Common;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Cache;

public class UserCache : EntityCache<AppUser, string>, IUserCache
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public UserCache(
        ICacheProvider cache,
        INetptuneUnitOfWork unitOfWork,
        ILogger<UserCache> logger)
        : base(cache, TimeSpan.FromHours(1), logger)
    {
        UnitOfWork = unitOfWork;
    }

    protected override Task<AppUser?> GetEntity(string key)
    {
        return UnitOfWork.Users.GetAsync(key);
    }

    protected override string GetCacheKey(string key)
    {
        return $"user:{key}";
    }
}
