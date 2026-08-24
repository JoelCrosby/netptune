using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage;

internal static class BrandingFileRelease
{
    public static async Task Release(
        INetptuneUnitOfWork unitOfWork,
        IStorageService storage,
        WorkspaceFile file,
        string userId,
        CancellationToken cancellationToken)
    {
        var released = await unitOfWork.Transaction(async () =>
        {
            var marked = await unitOfWork.WorkspaceFiles.TryMarkQuotaReleased(file.Id, userId, cancellationToken);

            if (!marked)
            {
                return false;
            }

            await unitOfWork.Workspaces.ReleaseStorage(file.WorkspaceId, file.SizeBytes, cancellationToken);

            return true;
        });

        if (!released)
        {
            return;
        }

        try
        {
            await storage.DeleteFileAsync(file.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The reconciliation job retries physical deletion.
        }
    }
}
