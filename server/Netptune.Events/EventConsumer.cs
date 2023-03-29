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

public sealed class EventConsumer : IEventConsumer, IDisposable
{
    private readonly RabbitMqOptions Options;
    private const string Queue = "netptune-events";

    private readonly Queue<Message> PendingMessages = new();
    private IConnection? Connection;
    private IModel? Channel;
    private EventingBasicConsumer? Consumer;

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

        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();

        Consumer = new (Channel);

        Channel.BasicConsume(queue: Queue, autoAck: true, consumer: Consumer);

        Consumer.Received += OnConsumerOnReceived;



        return Task.CompletedTask;
    }

    private void OnConsumerOnReceived(object? _, BasicDeliverEventArgs message)
    {
        var content = message.Body.ToArray();
        var body = Encoding.UTF8.GetString(content);

        var pendingMessage = new Message { Type = message.RoutingKey, Payload = body, };

        PendingMessages.Enqueue(pendingMessage);
    }

    public void Dispose()
    {
        Channel?.Close();
        Connection?.Close();
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
