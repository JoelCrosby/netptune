using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Services;

public sealed class ExportRetentionService : BackgroundService
{
    private const int ExpiryBatchSize = 200;
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleRunningAfter = TimeSpan.FromHours(2);

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<ExportRetentionService> Logger;

    public ExportRetentionService(IServiceScopeFactory scopeFactory, ILogger<ExportRetentionService> logger)
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
                Logger.LogError(exception, "[Export] retention sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task Sweep(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var exportJobs = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        await ExpireArtefacts(unitOfWork, exportJobs, storage, cancellationToken);
        await FailStaleJobs(unitOfWork, exportJobs, cancellationToken);
    }

    private async Task ExpireArtefacts(
        INetptuneUnitOfWork unitOfWork,
        IExportJobRepository exportJobs,
        IStorageService storage,
        CancellationToken cancellationToken)
    {
        var expired = await exportJobs.GetExpired(DateTime.UtcNow, ExpiryBatchSize, cancellationToken);

        foreach (var job in expired)
        {
            await ReleaseArtefact(unitOfWork, storage, job, cancellationToken);
        }

        if (expired.Count > 0)
        {
            Logger.LogInformation("[Export] expired {Count} artefacts", expired.Count);
        }
    }

    private static async Task ReleaseArtefact(
        INetptuneUnitOfWork unitOfWork,
        IStorageService storage,
        ExportJob job,
        CancellationToken cancellationToken)
    {
        var storageKey = job.StorageKey;
        var reclaimedBytes = job.QuotaReleased ? 0 : job.SizeBytes ?? 0;

        job.Status = ExportJobStatus.Expired;
        job.StorageKey = null;
        job.QuotaReleased = true;
        job.ProgressMessage = "Expired";

        await unitOfWork.CompleteAsync(cancellationToken);

        if (reclaimedBytes > 0)
        {
            await unitOfWork.Workspaces.ReleaseStorage(job.WorkspaceId, reclaimedBytes, cancellationToken);
        }

        if (storageKey is null)
        {
            return;
        }

        try
        {
            await storage.DeleteFileAsync(storageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The next sweep retries physical deletion.
        }
    }

    private async Task FailStaleJobs(
        INetptuneUnitOfWork unitOfWork,
        IExportJobRepository exportJobs,
        CancellationToken cancellationToken)
    {
        var startedBefore = DateTime.UtcNow.Subtract(StaleRunningAfter);
        var stale = await exportJobs.GetStaleRunning(startedBefore, cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        foreach (var job in stale)
        {
            job.Status = ExportJobStatus.Failed;
            job.Error = "The export did not finish and was abandoned.";
            job.ProgressMessage = "Abandoned";
            job.CompletedAt = DateTime.UtcNow;
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        Logger.LogWarning("[Export] abandoned {Count} stale running jobs", stale.Count);
    }
}
