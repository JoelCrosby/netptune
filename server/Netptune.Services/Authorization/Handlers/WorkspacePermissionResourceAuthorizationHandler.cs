using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers;

public class WorkspacePermissionResourceAuthorizationHandler : AuthorizationHandler<WorkspacePermissionRequirement>
{
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache Cache;
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspacePermissionResourceAuthorizationHandler(
        IIdentityService identity,
        IWorkspacePermissionCache cache,
        INetptuneUnitOfWork unitOfWork)
    {
        Identity = identity;
        Cache = cache;
        UnitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspacePermissionRequirement requirement)
    {
        var workspaceKey = Identity.TryGetWorkspaceKey();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = Identity.GetCurrentUserId();
            var workspaceUser = await Cache.GetUserPermissions(userId, workspaceKey);

            if (workspaceUser is null)
            {
                context.Fail();
                return;
            }

            if (workspaceUser.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
            {
                context.Succeed(requirement);
            }

            if (workspaceUser.Permissions.Contains(requirement.Permission) == true)
            {
                context.Succeed(requirement);
            }

            return;
        }

        // Unauthenticated — only allow read permissions on public workspaces.
        if (!IsReadPermission(requirement.Permission))
        {
            context.Fail();
            return;
        }

        if (workspaceKey is null) return;

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, isReadonly: true);

        if (workspace?.IsPublic == true)
        {
            context.Succeed(requirement);
        }
    }

    private static bool IsReadPermission(string permission)
    {
        return permission.EndsWith(".read", StringComparison.OrdinalIgnoreCase);
    }
}
