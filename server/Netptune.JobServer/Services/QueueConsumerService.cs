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

    private readonly Dictionary<string, Type> TypeSet = GetTypeMap();

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
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var eventMessage in EventConsumer.GetEventMessages(stoppingToken).WithCancellation(stoppingToken))
            {
                var messageType = GetType(eventMessage.Type);

                if (messageType is null)
                {
                    Logger.LogError("received unknown message type of {Type}", eventMessage.Type);
                    continue;
                }

                var message = (IEventMessage) JsonSerializer.Deserialize(eventMessage.Payload, messageType)!;

                Logger.LogInformation("received message type of {Type} {Payload}", eventMessage.Type, eventMessage.Payload);

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
        return TypeSet.TryGetValue(name, out var cached) ? cached : null;
    }

    private static Dictionary<string, Type> GetTypeMap()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith(nameof(Netptune)) ?? false)
            .SelectMany(a => a.GetTypes())
            .DistinctBy(a => a.FullName)
            .ToDictionary(k => k.FullName!, v => v);
    }
}
