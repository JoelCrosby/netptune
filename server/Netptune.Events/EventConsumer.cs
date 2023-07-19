using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NetMQ;
using NetMQ.Sockets;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class EventConsumer : IEventConsumer
{
    private readonly ILogger<EventConsumer> Logger;
    private readonly SubscriberSocket Subscriber;

    public EventConsumer(IOptions<MessageQueueOptions> options, ILogger<EventConsumer> logger)
    {
        Logger = logger;
        Subscriber = new(options.Value.ConnectionString);
    }

    public async IAsyncEnumerable<EventMessage> GetEventMessages([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Subscriber.Subscribe(MessageKeys.RoutingKey);

        while (!cancellationToken.IsCancellationRequested)
        {
            var _ = Subscriber.ReceiveFrameString();
            var type = Subscriber.ReceiveFrameString();
            var message = Subscriber.ReceiveFrameString();

            var pendingMessage = new EventMessage
            {
                Type = type,
                Payload = message,
            };

            Logger.LogInformation("[Event] type {Type} consumed: {Payload}", type, message);

            yield return pendingMessage;

            await Task.Delay(1000, cancellationToken);
        }
    }
}
