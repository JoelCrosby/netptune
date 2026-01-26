using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Services.Authentication;
using Netptune.Services.Authorization.Handlers;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static void AddNeptuneAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .Build())
            .AddPolicy(NetptunePolicies.Workspace, builder => builder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .AddRequirements(new WorkspaceRequirement())
                .Build())
            .AddPolicy(NetptunePolicies.Github, builder => builder
                .AddAuthenticationSchemes(AuthenticationSchemes.Github)
                .AddRequirements(new GithubRequirement())
                .Build());

        services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, WorkspaceResourceAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, GithubAuthorizationHandler>();
    }
}
