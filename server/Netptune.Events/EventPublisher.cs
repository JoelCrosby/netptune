using System.Text.Json;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> Logger;
    private readonly IProducer<string, string> Producer;

    public EventPublisher(ILogger<EventPublisher> logger, IProducer<string, string> producer)
    {
        Logger = logger;
        Producer = producer;
    }

    public async Task Dispatch<TPayload>(TPayload payload) where TPayload : class
    {
        var json = JsonSerializer.Serialize(payload);
        var type = typeof(TPayload).FullName;

        ArgumentException.ThrowIfNullOrEmpty(type);

        var message = new Message<string, string>
        {
            Key =  type,
            Value = json,
        };

        await Producer.ProduceAsync(MessageKeys.RoutingKey, message);

        Logger.LogInformation("[Event] type {Type} published: {Payload}", type, json);
    }
}
