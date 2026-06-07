using System.Text.Json;

using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly INatsJSContext JetStream;
    private readonly ILogger<EventPublisher> Logger;
    private readonly SemaphoreSlim StreamLock = new(1, 1);

    private bool StreamReady;

    public EventPublisher(INatsJSContext jetStream, ILogger<EventPublisher> logger)
    {
        JetStream = jetStream;
        Logger = logger;
    }

    public async Task Dispatch<TPayload>(TPayload payload) where TPayload : class
    {
        await EnsureStream();

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

    private async Task EnsureStream()
    {
        if (StreamReady) return;

        await StreamLock.WaitAsync();

        try
        {
            if (StreamReady) return;

            await JetStream.CreateOrUpdateStreamAsync(new StreamConfig
            {
                Name = MessageKeys.Queue,
                Subjects = [MessageKeys.RoutingKey],
                Storage = StreamConfigStorage.Memory,
            });

            StreamReady = true;
        }
        finally
        {
            StreamLock.Release();
        }
    }
}
