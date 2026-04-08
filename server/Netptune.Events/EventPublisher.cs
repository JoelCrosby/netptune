using System.Text.Json;

using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly INatsJSContext JetStream;
    private readonly ILogger<EventPublisher> Logger;

    public EventPublisher(INatsJSContext jetStream, ILogger<EventPublisher> logger)
    {
        JetStream = jetStream;
        Logger = logger;
    }

    public async Task Dispatch<TPayload>(TPayload payload) where TPayload : class
    {
        var type = typeof(TPayload).FullName!;
        var json = JsonSerializer.Serialize(payload);

        var message = new EventMessage
        {
            Type = type,
            Payload = json,
        };

        var ack = await JetStream.PublishAsync(MessageKeys.RoutingKey, message);

        ack.EnsureSuccess();

        Logger.LogInformation("[Event] type {Type} published", type);
    }
}
