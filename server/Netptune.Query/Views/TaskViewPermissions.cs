using Netptune.Core.Authorization;
using Netptune.Core.Cache;

namespace Netptune.Query.Views;

public static class TaskViewPermissions
{
    public static async Task<bool> CanManageShared(IWorkspacePermissionCache permissionCache, string userId, string? workspaceKey)
    {
        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        if (permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
        {
            return true;
        }

        return permissions?.Permissions.Contains(NetptunePermissions.TaskViews.ManageShared) == true;
    }
}
