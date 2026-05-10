using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Authentication;
using Netptune.Authentication.Authorization.Handlers;
using Netptune.Authentication.Authorization.Requirements;

namespace Netptune.Authentication.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static void AddNeptuneAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, NetptuneAuthorizationPolicyProvider>();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .Build())
            .AddPolicy(AuthenticationSchemes.Github, builder => builder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationSchemes.Github)
                .Build())
            .AddPolicy(AuthenticationSchemes.Google, builder => builder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationSchemes.Google)
                .Build())
            .AddPolicy(AuthenticationSchemes.Microsoft, builder => builder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationSchemes.Microsoft)
                .Build())
            .AddPolicy(NetptunePolicies.Workspace, builder => builder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .AddRequirements(new WorkspaceRequirement())
                .Build());

        services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, WorkspacePermissionResourceAuthorizationHandler>();
    }
}
