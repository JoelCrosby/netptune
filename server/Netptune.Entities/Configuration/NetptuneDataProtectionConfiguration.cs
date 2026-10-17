using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Entities.Contexts;

namespace Netptune.Entities.Configuration;

public static class NetptuneDataProtectionConfiguration
{
    private const string ApplicationName = "netptune";

    // Every host that protects or unprotects data must use this, so they share one key ring: the App
    // writes source control connect state and credentials that JobServer reads.
    public static IServiceCollection AddNetptuneDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<DataContext>();

        return services;
    }
}
