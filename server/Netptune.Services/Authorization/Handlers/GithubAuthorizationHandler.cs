using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization.Handlers;

public class GithubAuthorizationHandler : AuthorizationHandler<GithubRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GithubRequirement requirement)
    {
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
