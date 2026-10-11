using Netptune.Core.Authorization;
using Netptune.Core.Cache;

namespace Netptune.Handlers.Pages;

public sealed record PageArchiveRights(bool Any, bool Own);

public static class PagePermissions
{
    public const string OthersPagesInsideMessage = "Some pages inside were created by other people. Only they or an admin can archive or restore them.";

    public static async Task<PageArchiveRights> GetArchiveRights(IWorkspacePermissionCache permissionCache, string userId, string workspaceKey)
    {
        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        if (permissions is null)
        {
            return new PageArchiveRights(false, false);
        }

        var isAdministrator = permissions.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        var canArchiveAny = isAdministrator || permissions.Permissions.Contains(NetptunePermissions.Pages.DeleteAny);
        var canArchiveOwn = permissions.Permissions.Contains(NetptunePermissions.Pages.Delete);

        return new PageArchiveRights(canArchiveAny, canArchiveOwn);
    }
}
