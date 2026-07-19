using System.Text.Json;

using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly INatsJSContext JetStream;
    private readonly EventStream Stream;
    private readonly ILogger<EventPublisher> Logger;

    public EventPublisher(INatsJSContext jetStream, EventStream stream, ILogger<EventPublisher> logger)
    {
        JetStream = jetStream;
        Stream = stream;
        Logger = logger;
    }

    public async Task Dispatch<TPayload>(TPayload payload) where TPayload : class, IEventMessage
    {
        await Stream.EnsureCreated();

        var type = typeof(TPayload).FullName!;
        var json = JsonSerializer.Serialize(payload);

        var subject = TPayload.Subject;

        var message = new EventMessage
        {
            Type = type,
            Payload = json,
        };

        var ack = await JetStream.PublishAsync(subject, message);

        ack.EnsureSuccess();

        Logger.LogInformation("[Event] type {Type} published to {Subject}", type, subject);
    }

    public async Task DispatchCanonical(CanonicalEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        await Stream.EnsureCanonicalCreated(cancellationToken);

        var subject = $"netptune.events.v1.{envelope.EventKey}";
        var message = new EventMessage
        {
            Type = typeof(CanonicalEventEnvelope).FullName!,
            Payload = JsonSerializer.Serialize(envelope),
        };

        var ack = await JetStream.PublishAsync(
            subject,
            message,
            opts: new NatsJSPubOpts { MsgId = envelope.EventId.ToString("N") },
            cancellationToken: cancellationToken);

        ack.EnsureSuccess();

        Logger.LogInformation("[Event] canonical event {EventId} published to {Subject}", envelope.EventId, subject);
    }
}
