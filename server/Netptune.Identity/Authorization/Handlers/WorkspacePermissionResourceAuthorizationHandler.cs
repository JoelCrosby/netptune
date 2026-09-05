using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Identity.Authorization.Requirements;

namespace Netptune.Identity.Authorization.Handlers;

public class WorkspacePermissionResourceAuthorizationHandler : AuthorizationHandler<WorkspacePermissionRequirement>
{
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache Cache;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IHttpContextAccessor Context;

    public WorkspacePermissionResourceAuthorizationHandler(
        IIdentityService identity,
        IWorkspacePermissionCache cache,
        INetptuneUnitOfWork unitOfWork,
        IHttpContextAccessor context)
    {
        Identity = identity;
        Cache = cache;
        UnitOfWork = unitOfWork;
        Context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspacePermissionRequirement requirement)
    {
        var requestedWorkspaceKey = context.Resource as string;
        var headerWorkspaceKey = Identity.TryGetWorkspaceKey();
        var workspaceKey = requestedWorkspaceKey ?? headerWorkspaceKey;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = Identity.GetCurrentUserId();
            var workspaceUser = await Cache.GetUserPermissions(userId, workspaceKey);

            if (workspaceUser is null)
            {
                context.Fail();
                return;
            }

            var credentialScopes = context.User
                .FindAll(NetptuneClaims.CredentialScope)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);

            if (context.User.HasClaim(claim => claim.Type == NetptuneClaims.CredentialId)
                && !credentialScopes.Contains(requirement.Permission))
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

        var canEverBePublic = NetptunePermissions.PublicReadable.Contains(requirement.Permission);

        if (!canEverBePublic)
        {
            context.Fail();
            return;
        }

        if (!RequestReadsOnly())
        {
            context.Fail();
            return;
        }

        if (workspaceKey is null)
        {
            return;
        }

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, isReadonly: true, cancellationToken: CancellationToken.None);

        if (workspace?.IsPublic != true)
        {
            return;
        }

        var publicPermissions = NetptunePermissions.ResolvePublicPermissions(workspace.PublicPermissions);
        var workspaceSharesPermission = publicPermissions.Contains(requirement.Permission);

        if (workspaceSharesPermission)
        {
            context.Succeed(requirement);
        }
    }

    // Fails closed when there is no request to inspect, so an authorization check made outside a
    // request never hands an anonymous caller a grant.
    private bool RequestReadsOnly()
    {
        var method = Context.HttpContext?.Request.Method;

        if (method is null)
        {
            return false;
        }

        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
    }
}
