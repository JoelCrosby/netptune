using Microsoft.Extensions.Logging;

using Netptune.Cache.Common;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Models;
using Netptune.Core.UnitOfWork;

namespace Netptune.Cache;

public class WorkspacePermissionCache : EntityCache<UserPermissions?, WorkspaceUserKey>, IWorkspacePermissionCache
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspacePermissionCache(
        ICacheProvider cache,
        INetptuneUnitOfWork unitOfWork,
        ILogger<WorkspacePermissionCache> logger)
        : base(cache, TimeSpan.FromMinutes(5), logger)
    {
        UnitOfWork = unitOfWork;
    }

    protected override Task<UserPermissions?> GetEntity(WorkspaceUserKey key)
    {
        return UnitOfWork.WorkspaceUsers.GetUserPermissions(key.UserId, key.WorkspaceKey);
    }

    protected override string GetCacheKey(WorkspaceUserKey key)
    {
        return $"workspace-permissions:{key.WorkspaceKey}:{key.UserId}";
    }

    public async Task<UserPermissions?> GetUserPermissions(string userId, string? workspaceKey)
    {
        if (workspaceKey is null) return null;

        return await Get(new WorkspaceUserKey
        {
            UserId = userId,
            WorkspaceKey = workspaceKey,
        });
    }
}
