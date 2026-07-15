using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.App.Services;

public sealed class WorkspaceFileReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<WorkspaceFileReconciliationService> Logger;

    public WorkspaceFileReconciliationService(IServiceScopeFactory scopeFactory, ILogger<WorkspaceFileReconciliationService> logger)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Workspace file reconciliation failed");
            }
        }
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var staleBefore = DateTime.UtcNow.AddMinutes(-10);
        var staleFiles = await unitOfWork.WorkspaceFiles.GetStalePending(staleBefore, cancellationToken);

        foreach (var file in staleFiles)
        {
            if (await storage.ExistsAsync(file.StorageKey, cancellationToken))
            {
                await unitOfWork.WorkspaceFiles.MarkReady(file.Id, cancellationToken);
            }
            else
            {
                await ReleaseFile(unitOfWork, storage, file, cancellationToken);
            }
        }

        var tombstoneKeys = await unitOfWork.WorkspaceFiles.GetTombstoneStorageKeys(cancellationToken);

        foreach (var key in tombstoneKeys)
        {
            if (await storage.ExistsAsync(key, cancellationToken))
            {
                await storage.DeleteFileAsync(key, cancellationToken);
            }
        }

        var workspaceIds = await unitOfWork.Workspaces.GetAllIds(cancellationToken);

        foreach (var workspaceId in workspaceIds)
        {
            await unitOfWork.Transaction(async () =>
            {
                var workspace = await unitOfWork.Workspaces.GetForStorageUpdate(workspaceId, cancellationToken);

                if (workspace is null)
                {
                    return;
                }

                var expected = await unitOfWork.WorkspaceFiles.GetExpectedStorageUsage(workspaceId, cancellationToken);

                if (workspace.StorageUsedBytes == expected)
                {
                    return;
                }

                Logger.LogWarning("Repairing workspace {WorkspaceId} storage usage from {Actual} to {Expected}", workspaceId, workspace.StorageUsedBytes, expected);
                await unitOfWork.Workspaces.SetStorageUsage(workspaceId, expected, cancellationToken);
            });
        }
    }

    private static async Task ReleaseFile(INetptuneUnitOfWork unitOfWork, IStorageService storage, WorkspaceFile file, CancellationToken cancellationToken)
    {
        await unitOfWork.Transaction(async () =>
        {
            var released = await unitOfWork.WorkspaceFiles.TryMarkQuotaReleased(file.Id, file.CreatedByUserId ?? string.Empty, cancellationToken);

            if (!released)
            {
                return;
            }

            await unitOfWork.TaskFiles.DeleteByWorkspaceFileId(file.Id, cancellationToken);
            await unitOfWork.Workspaces.ReleaseStorage(file.WorkspaceId, file.SizeBytes, cancellationToken);
        });

        try
        {
            await storage.DeleteFileAsync(file.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The next reconciliation pass retries physical deletion.
        }
    }
}
