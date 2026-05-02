using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers;

public class WorkspaceAuthorizationHandler : AuthorizationHandler<WorkspaceRequirement>
{
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache Cache;
    private readonly INetptuneUnitOfWork UnitOfWork;

    public WorkspaceAuthorizationHandler(IIdentityService identity, IWorkspaceUserCache cache, INetptuneUnitOfWork unitOfWork)
    {
        Identity = identity;
        Cache = cache;
        UnitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspaceRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var workspaceClaim = context.User.Claims.FirstOrDefault(claim => claim.Type == NetptuneClaims.Workspace);
            var workspaceKey = workspaceClaim?.Value;

            if (workspaceKey is null) return;

            var userId = Identity.GetCurrentUserId();
            var memberOfWorkspace = await Cache.IsUserInWorkspace(userId, workspaceKey);

            if (memberOfWorkspace)
            {
                context.Succeed(requirement);
            }

            return;
        }

        // Unauthenticated — allow through if the workspace is marked public.
        // WorkspacePermissionResourceAuthorizationHandler will then enforce read-only.
        var workspaceKeyFromHeader = Identity.TryGetWorkspaceKey();

        if (workspaceKeyFromHeader is null) return;

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKeyFromHeader, isReadonly: true, cancellationToken: CancellationToken.None);

        if (workspace?.IsPublic == true)
        {
            context.Succeed(requirement);
        }
    }
}
