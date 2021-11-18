using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Models.Options;

namespace Netptune.JobServer.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneJobServerEntities(this IServiceCollection services, Action<NetptuneEntitiesOptions> configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var entityOptions = new NetptuneEntitiesOptions();
        configuration(entityOptions);

        services.Configure(configuration);

        services.AddScoped<DbContext, NetptuneJobContext>();
        services.AddDbContext<NetptuneJobContext>(options =>
        {
            options
                .UseNpgsql(entityOptions.ConnectionString)
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}