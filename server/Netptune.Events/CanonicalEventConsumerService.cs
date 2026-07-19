using System.Text.Json;

using Mediator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class CanonicalEventConsumerService : BackgroundService
{
    private const string DurableName = "netptune-activity-canonical-v1";

    private readonly INatsJSContext JetStream;
    private readonly EventStream Stream;
    private readonly EventMessageProcessor Processor;
    private readonly EventRetryPolicy RetryPolicy;
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private readonly ILogger<CanonicalEventConsumerService> Logger;

    public CanonicalEventConsumerService(
        INatsJSContext jetStream,
        EventStream stream,
        EventMessageProcessor processor,
        EventRetryPolicy retryPolicy,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CanonicalEventConsumerService> logger)
    {
        JetStream = jetStream;
        Stream = stream;
        Processor = processor;
        RetryPolicy = retryPolicy;
        ServiceScopeFactory = serviceScopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Stream.EnsureCanonicalCreated(stoppingToken);

        var consumer = await JetStream.CreateOrUpdateConsumerAsync(
            MessageKeys.CanonicalQueue,
            new ConsumerConfig
            {
                Name = DurableName,
                DurableName = DurableName,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                AckWait = RetryPolicy.AckWait,
                MaxDeliver = RetryPolicy.MaxDeliver,
                Backoff = RetryPolicy.Backoff,
                FilterSubjects = [MessageKeys.Subjects.Canonical],
            },
            stoppingToken);

        Logger.LogInformation("[Event] canonical consumer {Consumer} bound to {Subject}",DurableName,  MessageKeys.Subjects.Canonical);

        await foreach (var message in consumer.ConsumeAsync<EventMessage>(cancellationToken: stoppingToken))
        {
            await Processor.Process(message, Handle, stoppingToken);
        }
    }

    private async ValueTask Handle(EventMessage eventMessage, CancellationToken cancellationToken)
    {
        if (eventMessage.Type != typeof(CanonicalEventEnvelope).FullName)
        {
            throw new UnknownMessageTypeException(eventMessage.Type);
        }

        var envelope = JsonSerializer.Deserialize<CanonicalEventEnvelope>(eventMessage.Payload)
            ?? throw new InvalidOperationException("The canonical event envelope could not be deserialized.");

        using var scope = ServiceScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(envelope, cancellationToken);
    }
}
