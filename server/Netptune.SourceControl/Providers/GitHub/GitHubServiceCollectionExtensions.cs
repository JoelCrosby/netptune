using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Providers.GitHub;

internal static class GitHubServiceCollectionExtensions
{
    private const string IncompleteConfiguration = $"{GitHubAppOptions.SectionName} is enabled but not fully configured.";

    private const string UnreadablePrivateKey = $"{GitHubAppOptions.SectionName}:PrivateKey is not a readable PEM private key.";

    public static IServiceCollection AddGitHubSourceControl(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(GitHubAppOptions.SectionName);
        var isEnabled = section.GetValue<bool>(nameof(GitHubAppOptions.Enabled));

        if (!isEnabled)
        {
            return services;
        }

        services.AddOptions<GitHubAppOptions>()
            .Bind(section)
            .Validate(options => options.IsComplete(), IncompleteConfiguration)
            .Validate(options => options.HasReadablePrivateKey(), UnreadablePrivateKey)
            .ValidateOnStart();

#pragma warning disable EXTEXP0001
        services.AddHttpClient(GitHubApi.ClientName)
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler(GitHubApi.ClientName, GitHubResilience.Configure);
#pragma warning restore EXTEXP0001

        services.AddSingleton<GitHubApi>();
        services.AddSingleton<GitHubAppTokenMinter>();
        services.AddSingleton<GitHubConnector>();
        services.AddSingleton<GitHubSourceControlClient>();
        services.AddSingleton<GitHubWebhookReader>();
        services.AddSingleton<ISourceControlProvider, GitHubSourceControlProvider>();

        return services;
    }
}
