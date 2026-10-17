using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.Core.SourceControl;
using Netptune.SourceControl.Connect;
using Netptune.SourceControl.Credentials;
using Netptune.SourceControl.Providers.GitHub;
using Netptune.SourceControl.Registry;
using Netptune.SourceControl.Repos;

namespace Netptune.SourceControl.Configuration;

public static class NetptuneSourceControlConfiguration
{
    // The host must register Data Protection with the shared key ring, and the Netptune repositories:
    // connect state and stored provider credentials are unreadable to a host with a different ring.
    public static IServiceCollection AddNetptuneSourceControl(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ScmConnectStateProtector>();
        services.AddSingleton<IScmCredentialProtector, ScmCredentialProtector>();
        services.AddSingleton<ISourceControlProviderRegistry, SourceControlProviderRegistry>();
        services.AddScoped<ISourceControlRepoSync, SourceControlRepoSync>();

        services.AddGitHubSourceControl(configuration);

        return services;
    }
}
