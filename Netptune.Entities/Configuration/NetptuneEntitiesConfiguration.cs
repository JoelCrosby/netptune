using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Entities.Contexts;

using System;

namespace Netptune.Entities.Configuration
{
    public static class NetptuneEntitiesConfiguration
    {
        public static IServiceCollection AddNetptuneEntities(this IServiceCollection services, Action<NetptuneEntitiesOptions> configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var netptuneEntitiesOptions = new NetptuneEntitiesOptions();
            configuration(netptuneEntitiesOptions);

            services.Configure(configuration);

            services.AddScoped<DbContext, DataContext>();
            services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(netptuneEntitiesOptions.ConnectionString);
            });

            return services;
        }
    }
}
