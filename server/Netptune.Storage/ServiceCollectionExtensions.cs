using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services;

namespace Netptune.Storage;

public static class ServiceCollectionExtensions
{
    public static void AddS3StorageService(this IServiceCollection services, Action<S3StorageOptions> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        services.AddOptions<S3StorageOptions>()
            .Configure(action)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IStorageService, S3StorageService>();
    }

    public static void AddWorkspaceFileReconciler(this IServiceCollection services)
    {
        services.AddScoped<WorkspaceFileReconciler>();
    }
}