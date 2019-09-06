using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Entities.Contexts;

namespace Netptune.Entities.Configuration
{
    public static class NetptuneEntitiesConfiguration
    {
        public static IServiceCollection AddNetptuneEntities(this IServiceCollection services, Action<NetptuneEntitesOptions> optionsAction)
        {
            if (optionsAction == null)
                throw new ArgumentNullException(nameof(optionsAction));

            var netptuneEntitesOptions = new NetptuneEntitesOptions();
            optionsAction(netptuneEntitesOptions);

            services.Configure(optionsAction);

            services.AddScoped<DbContext, DataContext>();
            services.AddDbContext<DataContext>(options =>
            {
                if (netptuneEntitesOptions.IsWindows)
                {
                    options.UseSqlServer(netptuneEntitesOptions.ConnectionString);
                }
                else
                {
                    options.UseNpgsql(netptuneEntitesOptions.ConnectionString);
                }
            });

            return services;
        }
    }
}
