using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

using Netptune.JobServer.Data;

namespace Netptune.JobServer.Auth
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNetptuneJobServerAuth(this IServiceCollection services)
        {
            services
                .AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddEntityFrameworkStores<NetptuneJobContext>();

            services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
            });

            services.AddRazorPages();

            return services;
        }
    }
}
