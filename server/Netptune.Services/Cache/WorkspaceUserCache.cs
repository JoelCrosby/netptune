using System;
using System.Threading.Tasks;

using Netptune.Core.Cache;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Cache.Common;

namespace Netptune.Services.Cache
{
    public class WorkspaceUserCache : EntityCache<bool, WorkspaceUserKey>, IWorkspaceUserCache
    {
        private readonly INetptuneUnitOfWork UnitOfWork;

        public WorkspaceUserCache(ICacheProvider cache, INetptuneUnitOfWork unitOfWork): base(cache, TimeSpan.FromHours(1))
        {
            UnitOfWork = unitOfWork;
        }

        protected override Task<bool> GetEntity(WorkspaceUserKey key)
        {
            return UnitOfWork.Users.IsUserInWorkspace(key.UserId, key.WorkspaceKey);
        }

        protected override string GetCacheKey(WorkspaceUserKey key)
        {
            return $"workspace:{key.WorkspaceKey}:{key.UserId}";
        }

        public Task<bool> IsUserInWorkspace(string userId, string workspaceKey)
        {
            return Get(new WorkspaceUserKey
            {
                UserId = userId,
                WorkspaceKey = workspaceKey,
            });
        }
    }
}
