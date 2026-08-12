using Netptune.Core.Repositories.Common;
using Netptune.Storage;

namespace Netptune.JobServer.Services;

public sealed class WorkspaceFileReconciliationService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<WorkspaceFileReconciliationService> Logger;

    public WorkspaceFileReconciliationService(IServiceScopeFactory scopeFactory, ILogger<WorkspaceFileReconciliationService> logger)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        using var timer = new PeriodicTimer(SweepInterval);

        do
        {
            try
            {
                await Sweep(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "[Storage] workspace file reconciliation failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Sweep(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var advisoryLock = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();

        await using var lease = await advisoryLock.TryAcquire(AdvisoryLockKeys.WorkspaceFileReconciliation, cancellationToken);

        if (lease is null)
        {
            Logger.LogDebug("[Storage] workspace file reconciliation skipped, another replica holds the lock");

            return;
        }

        var reconciler = scope.ServiceProvider.GetRequiredService<WorkspaceFileReconciler>();

        await reconciler.Reconcile(cancellationToken);
    }
}
