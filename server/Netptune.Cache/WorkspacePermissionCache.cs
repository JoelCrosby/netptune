using Microsoft.Extensions.Logging;

using Netptune.Cache.Common;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.UnitOfWork;

namespace Netptune.Cache;

public class WorkspacePermissionCache : EntityCache<HashSet<string>?, WorkspaceUserKey>, IWorkspacePermissionCache
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspacePermissionCache(
        ICacheProvider cache,
        INetptuneUnitOfWork unitOfWork,
        ILogger<WorkspacePermissionCache> logger)
        : base(cache, TimeSpan.FromHours(1), logger)
    {
        UnitOfWork = unitOfWork;
    }

    protected override Task<HashSet<string>?> GetEntity(WorkspaceUserKey key)
    {
        return UnitOfWork.WorkspaceUsers.GetUserPermissions(key.UserId, key.WorkspaceKey);
    }

    protected override string GetCacheKey(WorkspaceUserKey key)
    {
        return $"workspace-permissions:{key.WorkspaceKey}:{key.UserId}";
    }

    public async Task<HashSet<string>?> GetUserPermissions(string userId, string? workspaceKey)
    {
        if (workspaceKey is null) return null;

        return await Get(new WorkspaceUserKey
        {
            UserId = userId,
            WorkspaceKey = workspaceKey,
        });
    }
}
