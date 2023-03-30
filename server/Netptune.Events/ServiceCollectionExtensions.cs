using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public static class ServiceCollectionExtensions
{
    public static void AddNetptuneRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> optionsAction)
    {
        if (optionsAction is null)
            throw new ArgumentNullException(nameof(optionsAction));

        var netptuneRepositoryOptions = new RabbitMqOptions();

        optionsAction(netptuneRepositoryOptions);

        services.Configure(optionsAction);

        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddSingleton<IEventConsumer, EventConsumer>();
    }
}
