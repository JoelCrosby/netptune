using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.Core.SourceControl;
using Netptune.SourceControl.Connect;
using Netptune.SourceControl.Providers.GitHub;
using Netptune.SourceControl.Registry;

namespace Netptune.SourceControl.Configuration;

public static class NetptuneSourceControlConfiguration
{
    // The host must register Data Protection with the shared key ring: connect state, and later
    // stored provider credentials, are unreadable to a host with a different ring.
    public static IServiceCollection AddNetptuneSourceControl(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ScmConnectStateProtector>();
        services.AddSingleton<ISourceControlProviderRegistry, SourceControlProviderRegistry>();

        services.AddGitHubSourceControl(configuration);

        return services;
    }
}
