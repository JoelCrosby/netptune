using Microsoft.Extensions.DependencyInjection;

namespace Netptune.Core.Events;

public static class ServiceCollectionExtensions
{
    public static void AddActivityLogger(this IServiceCollection services)
    {
        services.AddTransient<IActivityLogger, DistributedActivityLogger>();
    }

    public static void AddActivitySink(this IServiceCollection services)
    {
        services.AddSingleton<IActivityObservable, ActivityObservable>();
        services.AddTransient<IActivityLogger, ActivityLogger>();
        services.AddHostedService<ActivityWriterService>();
    }
}