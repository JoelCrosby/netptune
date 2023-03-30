using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Netptune.Core.Events;

namespace Netptune.JobServer.Services;

public sealed class QueueConsumerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IEventConsumer EventConsumer;
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private readonly ILogger<QueueConsumerService> Logger;

    private Dictionary<string, Type> TypeCache = new();

    public QueueConsumerService(
        IEventConsumer eventConsumer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<QueueConsumerService> logger)
    {
        EventConsumer = eventConsumer;
        ServiceScopeFactory = serviceScopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EventConsumer.Connect();

        while (!stoppingToken.IsCancellationRequested)
        {
            var eventMessages = await EventConsumer.GetEventMessages();

            foreach (var eventMessage in eventMessages)
            {
                var messageType = GetType(eventMessage.Type);

                if (messageType is null)
                {
                    Logger.LogError("received unknown message type of {Type}", eventMessage.Type);
                    continue;
                }

                var message = (IEventMessage) JsonSerializer.Deserialize(eventMessage.Payload, messageType)!;

                try
                {
                    using var scope = ServiceScopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(message, stoppingToken);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "processing message of type {Type} failed", messageType.Name);
                }
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private Type? GetType(string name)
    {
        if (TypeCache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var result = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith(nameof(Netptune)) ?? false)
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == name);

        if (result is {})
        {
            TypeCache.TryAdd(name, result);
        }

        return result;
    }
}
