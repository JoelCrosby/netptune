using Microsoft.Extensions.DependencyInjection;

using Netptune.Handlers.UserPreferences;

namespace Netptune.Handlers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneHandlers(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
        });

        services.AddScoped<PreferenceValueResolver>();

        return services;
    }
}
