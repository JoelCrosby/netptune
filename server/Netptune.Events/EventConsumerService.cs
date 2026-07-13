using System.Text.Json;

using Mediator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;
using Netptune.Events.Configuration;

namespace Netptune.Events;

public sealed class EventConsumerService : BackgroundService
{
    private readonly INatsJSContext JetStream;
    private readonly EventStream Stream;
    private readonly EventMessageProcessor Processor;
    private readonly EventRetryPolicy RetryPolicy;
    private readonly EventConsumerOptions Options;
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private readonly ILogger<EventConsumerService> Logger;

    private static readonly Dictionary<string, Type> MessageTypes = GetMessageTypes();

    public EventConsumerService(
        INatsJSContext jetStream,
        EventStream stream,
        EventMessageProcessor processor,
        EventRetryPolicy retryPolicy,
        IOptions<EventConsumerOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EventConsumerService> logger)
    {
        JetStream = jetStream;
        Stream = stream;
        Processor = processor;
        RetryPolicy = retryPolicy;
        Options = options.Value;
        ServiceScopeFactory = serviceScopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Stream.EnsureCreated(stoppingToken);

        var consumer = await JetStream.CreateOrUpdateConsumerAsync(MessageKeys.Queue, new ConsumerConfig
        {
            Name = Options.DurableName,
            DurableName = Options.DurableName,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            AckWait = RetryPolicy.AckWait,
            MaxDeliver = RetryPolicy.MaxDeliver,
            Backoff = RetryPolicy.Backoff,
            FilterSubjects = Options.FilterSubjects,
        }, stoppingToken);

        Logger.LogInformation(
            "[Event] consumer {Consumer} bound to {Subjects}",
            Options.DurableName,
            string.Join(", ", Options.FilterSubjects));

        await foreach (var message in consumer.ConsumeAsync<EventMessage>(cancellationToken: stoppingToken))
        {
            Logger.LogInformation("[Event] type {Type} consumed", message.Data?.Type);

            await Processor.Process(message, Handle, stoppingToken);
        }
    }

    private async ValueTask Handle(EventMessage eventMessage, CancellationToken cancellationToken)
    {
        if (!MessageTypes.TryGetValue(eventMessage.Type, out var messageType))
        {
            throw new UnknownMessageTypeException(eventMessage.Type);
        }

        var message = JsonSerializer.Deserialize(eventMessage.Payload, messageType)!;

        Logger.LogInformation("[Event] received message type of {Type} {Payload}", eventMessage.Type, eventMessage.Payload);

        using var scope = ServiceScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(message, cancellationToken);
    }

    private static Dictionary<string, Type> GetMessageTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.FullName?.StartsWith(nameof(Netptune)) ?? false)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IEventMessage).IsAssignableFrom(type) && !type.IsAbstract)
            .DistinctBy(type => type.FullName)
            .ToDictionary(type => type.FullName!, type => type);
    }
}
