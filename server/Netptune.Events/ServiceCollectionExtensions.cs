using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using NATS.Client.Core;
using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;
using Netptune.Events.Configuration;

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

        services.AddSingleton<EventStream>();
        services.AddSingleton<IEventPublisher, EventPublisher>();
    }

    public static void AddNetptuneMessageQueue(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        string durableName)
    {
        services.AddNetptuneMessageQueue(connectionString, options =>
        {
            configuration.GetSection(EventConsumerOptions.SectionName).Bind(options);

            options.DurableName = durableName;
        });
    }

    public static void AddNetptuneMessageQueue(
        this IServiceCollection services,
        string connectionString,
        Action<EventConsumerOptions> configure)
    {
        var options = new EventConsumerOptions();

        configure(options);
        options.Validate();

        services.AddNetptuneMessageQueue(connectionString);

        services.AddSingleton(Options.Create(options));

        services.TryAddSingleton(EventRetryPolicy.Default);

        services.AddSingleton<EventMessageProcessor>();
        services.AddHostedService<EventConsumerService>();
    }

    public static IServiceCollection AddCanonicalEventConsumer(this IServiceCollection services)
    {
        services.AddHostedService<CanonicalEventConsumerService>();

        return services;
    }
}
