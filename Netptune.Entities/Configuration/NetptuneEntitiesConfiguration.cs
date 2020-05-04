using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Entities.Contexts;

namespace Netptune.Entities.Configuration
{
    public static class NetptuneEntitiesConfiguration
    {
        public static IServiceCollection AddNetptuneEntities(this IServiceCollection services, Action<NetptuneEntitiesOptions> optionsAction)
        {
            if (optionsAction == null)
                throw new ArgumentNullException(nameof(optionsAction));

            var netptuneEntitiesOptions = new NetptuneEntitiesOptions();
            optionsAction(netptuneEntitiesOptions);

            services.Configure(optionsAction);

            services.AddScoped<DbContext, DataContext>();
            services.AddDbContext<DataContext>(options =>
            {
                //options.UseSqlServer(netptuneEntitiesOptions.ConnectionString);
                options.UseSqlite("Data Source=netptune.sqlite");
            });

            return services;
        }
    }
}
