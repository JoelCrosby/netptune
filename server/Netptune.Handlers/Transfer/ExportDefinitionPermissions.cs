using Netptune.Core.Authorization;
using Netptune.Core.Cache;

namespace Netptune.Handlers.Transfer;

internal static class ExportDefinitionPermissions
{
    public static async Task<bool> CanManage(IWorkspacePermissionCache permissionCache, string userId, string? workspaceKey)
    {
        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        if (permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
        {
            return true;
        }

        return permissions?.Permissions.Contains(NetptunePermissions.Data.ManageDefinitions) == true;
    }
}
