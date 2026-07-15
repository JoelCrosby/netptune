using Netptune.App.Services;
using Netptune.Core.Models.Options;

namespace Netptune.App.Configuration;

public static class WorkspaceStorageConfiguration
{
    private const string SectionName = "Storage";

    public static IServiceCollection AddNetptuneWorkspaceStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkspaceStorageOptions>(configuration.GetSection(SectionName));
        services.AddHostedService<WorkspaceFileReconciliationService>();

        return services;
    }
}
