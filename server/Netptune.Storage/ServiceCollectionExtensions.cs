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
            .Validate(
                options => !options.CloudFront.IsEnabled || options.CloudFront.HasHttpsBaseUrl,
                "Storage:S3:CloudFront:BaseUrl must be an absolute https URL.")
            .Validate(
                options => !options.CloudFront.IsEnabled || options.CloudFront.HasSigningKey(),
                "Storage:S3:CloudFront:BaseUrl requires KeyPairId and a PEM-encoded RSA PrivateKey.")
            .ValidateOnStart();

        services.AddSingleton<IStorageService, S3StorageService>();
    }

    public static void AddWorkspaceFileReconciler(this IServiceCollection services)
    {
        services.AddScoped<WorkspaceFileReconciler>();
    }
}
