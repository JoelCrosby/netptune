using System;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Models.Options;
using Netptune.Entities.Contexts;

using Npgsql;

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

        #pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
        #pragma warning restore CS0618 // Type or member is obsolete

        var netptuneEntitiesOptions = new NetptuneEntitiesOptions();
        configuration(netptuneEntitiesOptions);

        services.Configure(configuration);

        services.AddHostedService<HostedDatabaseService>();
        services.AddDbContext<DataContext>(options =>
        {
            options
                .UseNpgsql(netptuneEntitiesOptions.ConnectionString)
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static IdentityBuilder AddNetptuneIdentityEntities(this IdentityBuilder builder)
    {
        return builder.AddEntityFrameworkStores<DataContext>();
    }
}
