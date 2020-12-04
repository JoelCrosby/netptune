using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers
{
    public class WorkspaceAuthorizationHandler : AuthorizationHandler<WorkspaceRequirement>
    {
        private readonly IIdentityService Identity;
        private readonly IWorkspaceUserCache Cache;

        public WorkspaceAuthorizationHandler(IIdentityService identity, IWorkspaceUserCache cache)
        {
            Identity = identity;
            Cache = cache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspaceRequirement requirement)
        {
            var workspaceClaim = context.User.Claims.FirstOrDefault(claim => claim.Type == NetptuneClaims.Workspace);
            var workspaceKey = workspaceClaim?.Value;

            if (workspaceKey is null)
            {
                return;
            }

            var userId = await Identity.GetCurrentUserId();
            var memberOfWorkspace = await Cache.IsUserInWorkspace(userId, workspaceKey);

            if (!memberOfWorkspace)
            {
                return;
            }

            context.Succeed(requirement);
        }
    }
}
