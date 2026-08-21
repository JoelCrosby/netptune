using Microsoft.AspNetCore.DataProtection;

using Netptune.Entities.Contexts;

namespace Netptune.App.Configuration;

public static class DataProtectionConfiguration
{
    private const string ApplicationName = "netptune";

    public static IHostApplicationBuilder AddNetptuneDataProtection(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<DataContext>();

        return builder;
    }
}
