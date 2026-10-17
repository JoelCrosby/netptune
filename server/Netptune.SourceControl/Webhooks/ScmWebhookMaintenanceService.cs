using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Netptune.Core.Services.Activity;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Webhooks;

// The App stores a delivery and then publishes it, so a crash between the two leaves a row nothing
// would ever process without this sweep.
internal sealed class ScmWebhookMaintenanceService : BackgroundService
{
    private const int MaxAttempts = 5;
    private const int BatchSize = 100;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StallThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);
    private static readonly TimeSpan CheckRunRetention = TimeSpan.FromDays(90);

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly TimeProvider Clock;
    private readonly ILogger<ScmWebhookMaintenanceService> Logger;

    public ScmWebhookMaintenanceService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<ScmWebhookMaintenanceService> logger)
    {
        ScopeFactory = scopeFactory;
        Clock = clock;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, Clock);

        do
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.LogWarning(exception, "Webhook delivery maintenance failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunOnce(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var now = Clock.GetUtcNow().UtcDateTime;

        var stalled = await unitOfWork.ScmWebhookDeliveries.GetStalled(
            now - StallThreshold,
            MaxAttempts,
            BatchSize,
            cancellationToken);

        foreach (var deliveryRecordId in stalled)
        {
            await publisher.Dispatch(new ScmWebhookReceivedMessage { DeliveryRecordId = deliveryRecordId });
        }

        var prunedProcessed = await unitOfWork.ScmWebhookDeliveries.DeleteProcessedBefore(
            now - Retention,
            cancellationToken);
        var prunedAbandoned = await unitOfWork.ScmWebhookDeliveries.DeleteAbandonedBefore(
            now - Retention,
            MaxAttempts,
            cancellationToken);
        var pruned = prunedProcessed + prunedAbandoned;
        var prunedCheckRuns = await unitOfWork.SourceControlCheckRuns.DeleteUpdatedBefore(
            now - CheckRunRetention,
            cancellationToken);

        var didWork = stalled.Count > 0 || pruned > 0 || prunedCheckRuns > 0;

        if (didWork)
        {
            Logger.LogInformation(
                "Re-published {Stalled} stalled webhook deliveries, pruned {Pruned} old ones and {PrunedCheckRuns} old check runs",
                stalled.Count,
                pruned,
                prunedCheckRuns);
        }
    }
}
