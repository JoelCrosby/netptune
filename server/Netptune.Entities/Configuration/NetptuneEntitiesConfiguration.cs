using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Models.Options;
using Netptune.Entities.Contexts;

namespace Netptune.Entities.Configuration;

public static class NetptuneEntitiesConfiguration
{
    public static IServiceCollection AddNetptuneEntities(this IServiceCollection services, Action<NetptuneEntitiesOptions> configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Use legacy Npgsql timestamp behavior
        // https://www.npgsql.org/doc/types/datetime.html#timestamps-and-timezones

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var netptuneEntitiesOptions = new NetptuneEntitiesOptions();
        configuration(netptuneEntitiesOptions);

        services.Configure(configuration);

        services.AddScoped<DbContext, DataContext>();
        services.AddDbContext<DataContext>(options =>
        {
            options
                .UseNpgsql(netptuneEntitiesOptions.ConnectionString)
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
