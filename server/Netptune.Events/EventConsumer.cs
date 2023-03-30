using System.Text;

using Microsoft.Extensions.Options;

using Netptune.Core.Events;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netptune.Events;

public sealed class EventConsumer : IEventConsumer, IDisposable
{
    private readonly RabbitMqOptions Options;

    private readonly Queue<EventMessage> PendingMessages = new();
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
            Uri = new Uri(Options.ConnectionString!),
            VirtualHost = "/",
            ClientProvidedName = "netptune-job-server",
        };

        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();

        Consumer = new (Channel);

        Channel.QueueDeclare(queue: MessageKeys.Queue, durable: false, exclusive: false, autoDelete: false);
        Channel.QueueBind(MessageKeys.Queue, MessageKeys.Exchange, MessageKeys.RoutingKey);
        Channel.BasicConsume(queue: MessageKeys.Queue, autoAck: false, consumer: Consumer);

        Consumer.Received += OnConsumerOnReceived;

        return Task.CompletedTask;
    }

    private void OnConsumerOnReceived(object? _, BasicDeliverEventArgs message)
    {
        var content = message.Body.ToArray();
        var body = Encoding.UTF8.GetString(content);

        var pendingMessage = new EventMessage
        {
            Type = message.BasicProperties.Type,
            Payload = body,
        };

        PendingMessages.Enqueue(pendingMessage);

        Channel?.BasicAck(message.DeliveryTag, false);
    }

    public void Dispose()
    {
        Channel?.Close();
        Connection?.Close();
    }

    public Task<IEnumerable<EventMessage>> GetEventMessages()
    {
        var result = PendingMessages.ToList().AsEnumerable();

        PendingMessages.Clear();

        return Task.FromResult(result);
    }
}
