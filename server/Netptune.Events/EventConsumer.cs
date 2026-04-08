using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class EventConsumer : IEventConsumer
{
    private readonly INatsJSContext JetStream;
    private readonly ILogger<EventConsumer> Logger;

    public EventConsumer(INatsJSContext jetStream, ILogger<EventConsumer> logger)
    {
        JetStream = jetStream;
        Logger = logger;
    }

    public async IAsyncEnumerable<EventMessage> GetEventMessages([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await JetStream.CreateOrUpdateStreamAsync(new StreamConfig
        {
            Name = MessageKeys.Queue,
            Subjects = [MessageKeys.RoutingKey],
            Storage = StreamConfigStorage.Memory,
        }, cancellationToken);

        var consumer = await JetStream.CreateOrUpdateConsumerAsync(MessageKeys.Queue, new ConsumerConfig
        {
            Name = MessageKeys.Consumer,
            DurableName = MessageKeys.Consumer,
        }, cancellationToken);

        await foreach (var msg in consumer.ConsumeAsync<EventMessage>(cancellationToken: cancellationToken))
        {
            if (msg.Data is null) continue;

            Logger.LogInformation("[Event] type {Type} consumed", msg.Data.Type);

            yield return msg.Data;

            await msg.AckAsync(cancellationToken: cancellationToken);
        }
    }
}
