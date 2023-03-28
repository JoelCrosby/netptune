using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Events;

namespace Netptune.Events;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddTransient<IEventPublisher, EventPublisher>();

        return services;
    }

    public static void AddNetptuneEvents(this IServiceCollection services, Action<RabbitMqOptions> optionsAction)
    {
        if (optionsAction is null)
            throw new ArgumentNullException(nameof(optionsAction));

        var netptuneRepositoryOptions = new RabbitMqOptions();

        optionsAction(netptuneRepositoryOptions);

        services.Configure(optionsAction);

        services.AddTransient<IEventPublisher, EventPublisher>();
        services.AddTransient<IEventConsumer, EventConsumer>();
    }
}
