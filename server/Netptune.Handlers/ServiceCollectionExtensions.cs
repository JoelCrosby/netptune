using Microsoft.Extensions.DependencyInjection;

namespace Netptune.Handlers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneHandlers(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
        });

        return services;
    }
}
