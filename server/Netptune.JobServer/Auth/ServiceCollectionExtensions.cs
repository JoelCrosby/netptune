using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

using Netptune.JobServer.Data;
using Netptune.JobServer.Util;

namespace Netptune.JobServer.Auth
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNetptuneJobServerAuth(this IServiceCollection services)
        {
            services
                .AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<NetptuneJobContext>()
                .AddDefaultTokenProviders()
                .AddSignInManager();

            services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
            });

            return services;
        }
    }
}
