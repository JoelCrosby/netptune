using System.Runtime.CompilerServices;

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

using NATS.Client.Core;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class EventConsumer : IEventConsumer
{
    private readonly ILogger<EventConsumer> Logger;
    private readonly IConsumer<string, string> Consumer;

    public EventConsumer(ILogger<EventConsumer> logger, IConsumer<string, string> consumer)
    {
        Logger = logger;
        Consumer = consumer;
    }

    public IEnumerable<EventMessage> GetEventMessages(CancellationToken cancellationToken)
    {
        Consumer.Subscribe([MessageKeys.RoutingKey]);

        while (!cancellationToken.IsCancellationRequested)
        {
            var msg = Consumer.Consume(cancellationToken);

            var payload = msg.Message.Value;
            var type = msg.Message.Key;

            if (type is null || payload is null)
            {
                throw new Exception("Unknown message type");
            }

            var pendingMessage = new EventMessage
            {
                Type = type,
                Payload = payload,
            };

            Logger.LogInformation("[Event] type {Type} consumed", type);

            yield return pendingMessage;
        }

        Consumer.Unsubscribe();
    }
}
