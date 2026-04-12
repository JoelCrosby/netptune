using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers;

public class WorkspacePermissionResourceAuthorizationHandler : AuthorizationHandler<WorkspacePermissionRequirement>
{
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache Cache;

    public WorkspacePermissionResourceAuthorizationHandler(IIdentityService identity, IWorkspacePermissionCache cache)
    {
        Identity = identity;
        Cache = cache;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspacePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        var workspaceKey = Identity.GetWorkspaceKey();

        var userId = Identity.GetCurrentUserId();
        var permissions = await Cache.GetUserPermissions(userId, workspaceKey);

        if (permissions?.Contains(requirement.Permission) == true)
        {
            context.Succeed(requirement);
        }
    }
}
