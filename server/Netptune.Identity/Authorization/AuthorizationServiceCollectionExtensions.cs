using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Identity.Authentication;
using Netptune.Identity.Authorization.Handlers;
using Netptune.Identity.Authorization.Requirements;

namespace Netptune.Identity.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneAuthorization(
        this IServiceCollection services,
        string authenticationScheme = AuthenticationSchemes.Smart)
    {
        services.Configure<NetptuneAuthorizationOptions>(options =>
            options.AuthenticationScheme = authenticationScheme);
        services.AddSingleton<IAuthorizationPolicyProvider, NetptuneAuthorizationPolicyProvider>();

        var authenticatedUser = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(authenticationScheme)
            .Build();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(authenticatedUser)

            // Endpoints carrying no authorization metadata fall back to requiring a signed-in
            // caller, so a route that forgets RequireAuthorization fails closed rather than open.
            .SetFallbackPolicy(authenticatedUser)
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
            .AddPolicy(NetptunePolicies.InteractiveUser, builder => builder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(authenticationScheme)
                .RequireClaim(NetptuneClaims.ActorType, AppUserType.User.ToString())
                .Build())
            .AddPolicy(NetptunePolicies.Workspace, builder => builder.RequireAuthenticatedUser()
                .AddAuthenticationSchemes(authenticationScheme)
                .AddRequirements(new WorkspaceRequirement())
                .Build());

        services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, WorkspacePermissionResourceAuthorizationHandler>();

        return services;
    }
}
