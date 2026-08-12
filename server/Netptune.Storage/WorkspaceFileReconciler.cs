using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Repositories;

namespace Netptune.Storage;

public sealed class WorkspaceFileReconciler
{
    private static readonly TimeSpan PendingGracePeriod = TimeSpan.FromMinutes(10);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportJobRepository ExportJobs;
    private readonly IStorageService Storage;
    private readonly ILogger<WorkspaceFileReconciler> Logger;

    public WorkspaceFileReconciler(
        INetptuneUnitOfWork unitOfWork,
        IExportJobRepository exportJobs,
        IStorageService storage,
        ILogger<WorkspaceFileReconciler> logger)
    {
        UnitOfWork = unitOfWork;
        ExportJobs = exportJobs;
        Storage = storage;
        Logger = logger;
    }

    public async Task Reconcile(CancellationToken cancellationToken)
    {
        await SettleStalePendingFiles(cancellationToken);
        await DeleteTombstonedObjects(cancellationToken);
        await RepairStorageUsage(cancellationToken);
    }

    private async Task SettleStalePendingFiles(CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.Subtract(PendingGracePeriod);
        var staleFiles = await UnitOfWork.WorkspaceFiles.GetStalePending(staleBefore, cancellationToken);

        foreach (var file in staleFiles)
        {
            var uploadLanded = await Storage.ExistsAsync(file.StorageKey, cancellationToken);

            if (uploadLanded)
            {
                await UnitOfWork.WorkspaceFiles.MarkReady(file.Id, cancellationToken);
            }
            else
            {
                await ReleaseFile(file, cancellationToken);
            }
        }
    }

    private async Task DeleteTombstonedObjects(CancellationToken cancellationToken)
    {
        var tombstoneKeys = await UnitOfWork.WorkspaceFiles.GetTombstoneStorageKeys(cancellationToken);

        foreach (var key in tombstoneKeys)
        {
            var objectRemains = await Storage.ExistsAsync(key, cancellationToken);

            if (objectRemains)
            {
                await Storage.DeleteFileAsync(key, cancellationToken);
            }
        }
    }

    private async Task RepairStorageUsage(CancellationToken cancellationToken)
    {
        var workspaceIds = await UnitOfWork.Workspaces.GetAllIds(cancellationToken);

        foreach (var workspaceId in workspaceIds)
        {
            await UnitOfWork.Transaction(async () =>
            {
                var workspace = await UnitOfWork.Workspaces.GetForStorageUpdate(workspaceId, cancellationToken);

                if (workspace is null)
                {
                    return;
                }

                var expectedFileUsage = await UnitOfWork.WorkspaceFiles.GetExpectedStorageUsage(workspaceId, cancellationToken);
                var expectedExportUsage = await ExportJobs.GetExpectedStorageUsage(workspaceId, cancellationToken);
                var expected = expectedFileUsage + expectedExportUsage;

                if (workspace.StorageUsedBytes == expected)
                {
                    return;
                }

                Logger.LogWarning("Repairing workspace {WorkspaceId} storage usage from {Actual} to {Expected}", workspaceId, workspace.StorageUsedBytes, expected);
                await UnitOfWork.Workspaces.SetStorageUsage(workspaceId, expected, cancellationToken);
            });
        }
    }

    private async Task ReleaseFile(WorkspaceFile file, CancellationToken cancellationToken)
    {
        await UnitOfWork.Transaction(async () =>
        {
            var released = await UnitOfWork.WorkspaceFiles.TryMarkQuotaReleased(file.Id, file.CreatedByUserId ?? string.Empty, cancellationToken);

            if (!released)
            {
                return;
            }

            await UnitOfWork.TaskFiles.DeleteByWorkspaceFileId(file.Id, cancellationToken);
            await UnitOfWork.Workspaces.ReleaseStorage(file.WorkspaceId, file.SizeBytes, cancellationToken);
        });

        try
        {
            await Storage.DeleteFileAsync(file.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The next reconciliation pass retries physical deletion.
        }
    }
}
