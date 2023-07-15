using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public static class ServiceCollectionExtensions
{
    public static void AddNetptuneMessageQueue(this IServiceCollection services, Action<MessageQueueOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var options = new MessageQueueOptions();

        action(options);

        services.Configure(action);

        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddSingleton<IEventConsumer, EventConsumer>();
    }
}
