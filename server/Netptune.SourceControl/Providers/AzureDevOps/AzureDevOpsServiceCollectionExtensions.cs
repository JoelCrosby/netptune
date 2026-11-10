using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal static class AzureDevOpsServiceCollectionExtensions
{
    private const string IncompleteConfiguration = $"{AzureDevOpsOptions.SectionName}:ApiBaseUrl is not an absolute URL.";

    public static IServiceCollection AddAzureDevOpsSourceControl(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(AzureDevOpsOptions.SectionName);
        var isEnabled = section.GetValue<bool>(nameof(AzureDevOpsOptions.Enabled));

        if (!isEnabled)
        {
            return services;
        }

        services.AddOptions<AzureDevOpsOptions>()
            .Bind(section)
            .Validate(options => options.IsComplete(), IncompleteConfiguration)
            .ValidateOnStart();

#pragma warning disable EXTEXP0001
        services.AddHttpClient(AzureDevOpsApi.ClientName)
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler(AzureDevOpsApi.ClientName, ScmHttpResilience.Configure);
#pragma warning restore EXTEXP0001

        services.AddSingleton<AzureDevOpsApi>();
        services.AddSingleton<AzureDevOpsConnectionChecker>();
        services.AddSingleton<AzureDevOpsConnector>();
        services.AddSingleton<AzureDevOpsSourceControlClient>();
        services.AddSingleton<ISourceControlProvider, AzureDevOpsSourceControlProvider>();

        return services;
    }
}
