using Microsoft.Extensions.DependencyInjection;

using NATS.Client.Core;
using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public static class ServiceCollectionExtensions
{
    public static void AddNetptuneMessageQueue(this IServiceCollection services, string connectionString)
    {

        var serializer = new NatsJsonContextSerializerRegistry(NatsJsonContext.Default);

        services.AddSingleton<INatsConnection>(_ => new NatsConnection(
                NatsOpts.Default with
                {
                    Url = connectionString,
                    SerializerRegistry = serializer,
                }
            )
        );

        services.AddSingleton<INatsJSContext>(sp =>
            new NatsJSContext(sp.GetRequiredService<INatsConnection>()));

        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddSingleton<IEventConsumer, EventConsumer>();
    }

}
