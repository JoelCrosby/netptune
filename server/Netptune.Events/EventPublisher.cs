using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NetMQ;
using NetMQ.Sockets;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

namespace Netptune.Events;

public class EventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<EventPublisher> Logger;
    private readonly PublisherSocket Publisher;

    public EventPublisher(IOptions<MessageQueueOptions> options, ILogger<EventPublisher> logger)
    {
        Publisher = new PublisherSocket(options.Value.ConnectionString);
        Logger = logger;
    }

    public Task Dispatch<TPayload>(TPayload payload) where TPayload : class
    {
        var json = JsonSerializer.Serialize(payload);

        Publisher
            .SendMoreFrame(MessageKeys.RoutingKey)
            .SendMoreFrame(typeof(TPayload).FullName ?? "")
            .SendFrame(json);

        Logger.LogInformation("event published: {Payload}", json);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Publisher.Dispose();
    }
}
