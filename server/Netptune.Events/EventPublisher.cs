using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;

using RabbitMQ.Client;

namespace Netptune.Events;

public class EventPublisher : IEventPublisher
{
    private readonly RabbitMqOptions Options;

    private IModel? Channel;
    private IConnection? Connection;

    public EventPublisher(IOptions<RabbitMqOptions> options)
    {
        Options = options.Value;
    }

    public Task Dispatch<TPayload>(TPayload payload) where TPayload : class
    {
        if (Channel is null)
        {
            Connect();

            ArgumentNullException.ThrowIfNull(Channel);
        }

        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);
        var routingKey = typeof(TPayload).Name;

        var props = Channel.CreateBasicProperties();

        props.Type = typeof(TPayload).FullName;

        Channel.QueueBind(MessageKeys.Queue, MessageKeys.Exchange, MessageKeys.RoutingKey);
        Channel.BasicPublish(exchange: MessageKeys.Exchange, routingKey, basicProperties: props, body);

        return Task.CompletedTask;
    }

    private void Connect()
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(Options.ConnectionString!),
            VirtualHost = "/",
            ClientProvidedName = MessageKeys.Client,
        };

        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();

        Channel.ExchangeDeclare(MessageKeys.Exchange, "direct");
        Channel.QueueDeclare(queue: MessageKeys.Queue, durable: false, exclusive: false, autoDelete: false);
    }
}
