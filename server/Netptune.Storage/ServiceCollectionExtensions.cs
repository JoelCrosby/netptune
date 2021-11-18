using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services;

namespace Netptune.Storage;

public static class ServiceCollectionExtensions
{
    public static void AddS3StorageService(this IServiceCollection services, Action<S3StorageOptions> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var options = new S3StorageOptions();
        action(options);

        services.Configure(action);
        services.AddSingleton<IStorageService, S3StorageService>();
    }
}