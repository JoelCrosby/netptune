using Netptune.Core.Cache.Common;
using Netptune.Core.Models;

namespace Netptune.Core.Cache;

public interface IWorkspacePermissionCache : IEntityCache<UserPermissions?, WorkspaceUserKey>
{
    Task<UserPermissions?> GetUserPermissions(string userId, string? workspaceKey);
}
