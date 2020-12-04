using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Services.Authorization.Handlers;
using Netptune.Services.Authorization.Requirements;

namespace Netptune.Services.Authorization
{
    public static class AuthorizationServiceCollectionExtensions
    {
        public static void AddNeptuneAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .Build();

                options.AddPolicy(NetptunePolicies.Workspace, builder => builder.RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .AddRequirements(new WorkspaceRequirement())
                    .Build());
            });

            services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, WorkspaceResourceAuthorizationHandler>();
        }
    }
}
