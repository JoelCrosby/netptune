using Microsoft.AspNetCore.Identity;
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

            services.AddRazorPages();

            return services;
        }
    }
}
