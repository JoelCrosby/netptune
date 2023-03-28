using System.Text;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netptune.Events;

public interface IEventConsumer
{
    Task Connect();

    Task<IEnumerable<Message>> GetMessages();
}

public class EventConsumer : IEventConsumer
{
    private readonly RabbitMqOptions Options;
    private const string Queue = "netptune-events";

    private readonly Queue<Message> PendingMessages = new();

    public EventConsumer(IOptions<RabbitMqOptions> options)
    {
        Options = options.Value;
    }

    public Task Connect()
    {
        var factory = new ConnectionFactory
        {
            HostName = Options.ConnectionString,
            VirtualHost = "/",
            UserName = "guest",
            Password = "guest",
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, message) =>
        {
            var content = message.Body.ToArray();
            var body = Encoding.UTF8.GetString(content);

            var pendingMessage = new Message
            {
                Type = message.RoutingKey,
                Payload = body,
            };

            PendingMessages.Enqueue(pendingMessage);
        };

        channel.BasicConsume(queue: Queue, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<Message>> GetMessages()
    {
        var result = PendingMessages.ToList().AsEnumerable();

        PendingMessages.Clear();

        return Task.FromResult(result);
    }
}

public record Message
{
    public required string Type { get; init; }

    public string? Payload { get; init; }
}
