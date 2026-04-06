using Microsoft.Extensions.Logging;

using Netptune.Cache.Common;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.UnitOfWork;

namespace Netptune.Cache;

public class WorkspaceUserCache : EntityCache<bool, WorkspaceUserKey>, IWorkspaceUserCache
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspaceUserCache(
        ICacheProvider cache,
        INetptuneUnitOfWork unitOfWork,
        ILogger<WorkspaceUserCache> logger)
        : base(cache, TimeSpan.FromHours(1), logger)
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
