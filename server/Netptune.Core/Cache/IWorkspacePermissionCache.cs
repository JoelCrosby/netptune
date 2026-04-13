using Netptune.Core.Cache.Common;

namespace Netptune.Core.Cache;

public interface IWorkspacePermissionCache : IEntityCache<HashSet<string>?, WorkspaceUserKey>
{
    Task<HashSet<string>?> GetUserPermissions(string userId, string? workspaceKey);
}
