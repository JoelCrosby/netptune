using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers;

public class WorkspaceResourceAuthorizationHandler : AuthorizationHandler<WorkspaceRequirement, string>
{
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache Cache;

    public WorkspaceResourceAuthorizationHandler(IIdentityService identity, IWorkspaceUserCache cache)
    {
        Identity = identity;
        Cache = cache;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspaceRequirement requirement, string? workspaceKey)
    {
        if (workspaceKey is null)
        {
            return;
        }

        var userId = Identity.GetCurrentUserId();
        var memberOfWorkspace = await Cache.IsUserInWorkspace(userId, workspaceKey);

        if (!memberOfWorkspace)
        {
            return;
        }

        context.Succeed(requirement);
    }
}
