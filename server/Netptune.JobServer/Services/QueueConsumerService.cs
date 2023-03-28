using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Netptune.Events;

namespace Netptune.JobServer.Services;

public class QueueConsumerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IEventConsumer EventConsumer;

    public QueueConsumerService(IEventConsumer eventConsumer)
    {
        EventConsumer = eventConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EventConsumer.Connect();

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await EventConsumer.GetMessages();

            foreach (var message in messages)
            {
                Console.WriteLine("received message: {0} {1}", message.Type, message.Payload);
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
