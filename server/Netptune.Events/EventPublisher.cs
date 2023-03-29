using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Core.Events;

using RabbitMQ.Client;

namespace Netptune.Events;

public class EventPublisher : IEventPublisher
{
    private readonly RabbitMqOptions Options;
    private const string Queue = "netptune-events";

    public EventPublisher(IOptions<RabbitMqOptions> options)
    {
        Options = options.Value;
    }

    public Task Dispatch<TPayload>(NetptuneEvent type, TPayload payload) where TPayload : class
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

        channel.QueueDeclare(queue: Queue, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);
        var routingKey = Enum.GetName(type);

        channel.BasicPublish(exchange: string.Empty, routingKey, basicProperties: null, body);

        return Task.CompletedTask;
    }
}
