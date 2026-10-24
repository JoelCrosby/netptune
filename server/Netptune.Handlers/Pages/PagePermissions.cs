using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Localization;
using Netptune.Core.Services;

namespace Netptune.Handlers.Pages;

public sealed record PageArchiveRights(bool Any, bool Own);

public static class PagePermissions
{
    public static string OthersPagesInsideMessage => AppText.ErrorPagesInsideCreatedOtherPeopleOnlyThey;

    public static async Task<PageArchiveRights> GetArchiveRights(
        IWorkspacePermissionCache permissionCache,
        IIdentityService identity,
        string userId,
        string workspaceKey)
    {
        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        if (permissions is null)
        {
            return new PageArchiveRights(false, false);
        }

        var isAdministrator = permissions.Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        var memberCanArchiveAny = isAdministrator || permissions.Permissions.Contains(NetptunePermissions.Pages.DeleteAny);
        var memberCanArchiveOwn = permissions.Permissions.Contains(NetptunePermissions.Pages.Delete);
        var credentialAllowsAny = !identity.IsRestrictedByCredential(NetptunePermissions.Pages.DeleteAny);
        var credentialAllowsOwn = !identity.IsRestrictedByCredential(NetptunePermissions.Pages.Delete);
        var canArchiveAny = memberCanArchiveAny && credentialAllowsAny;
        var canArchiveOwn = memberCanArchiveOwn && credentialAllowsOwn;

        return new PageArchiveRights(canArchiveAny, canArchiveOwn);
    }
}
