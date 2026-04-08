using Microsoft.Extensions.DependencyInjection;

using NATS.Client.Core;
using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public static class ServiceCollectionExtensions
{
    public static void AddNetptuneMessageQueue(this IServiceCollection services)
    {
        services.AddSingleton<INatsJSContext>(sp =>
            sp.GetRequiredService<INatsConnection>().CreateJetStreamContext());

        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddSingleton<IEventConsumer, EventConsumer>();
    }
}
