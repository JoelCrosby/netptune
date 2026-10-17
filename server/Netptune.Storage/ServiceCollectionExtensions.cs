using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services;

namespace Netptune.Storage;

public static class ServiceCollectionExtensions
{
    public static void AddS3StorageService(this IServiceCollection services)
    {
        services.AddOptions<S3StorageOptions>()
            .BindConfiguration(S3StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IStorageService, S3StorageService>();
    }

    public static void AddWorkspaceFileReconciler(this IServiceCollection services)
    {
        services.AddScoped<WorkspaceFileReconciler>();
    }
}
