using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record DeleteExportJobCommand(Guid PublicId) : IRequest<ClientResponse>;

public sealed class DeleteExportJobCommandHandler : IRequestHandler<DeleteExportJobCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public DeleteExportJobCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IStorageService storage,
        IExportJobRepository exportJobs)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ClientResponse> Handle(DeleteExportJobCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var job = await ExportJobs.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (job is null)
        {
            return ClientResponse.NotFound;
        }

        var isDeletable = ExportJobStatuses.CanDelete(job.Status);

        if (!isDeletable)
        {
            return ClientResponse.Failed($"An export that is {job.Status} cannot be deleted. Cancel it first.");
        }

        var storageKey = job.StorageKey;
        var reclaimedBytes = job.QuotaReleased ? 0 : job.SizeBytes ?? 0;

        job.StorageKey = null;
        job.QuotaReleased = true;
        job.Delete(Identity.GetCurrentUserId());

        await UnitOfWork.CompleteAsync(cancellationToken);

        if (reclaimedBytes > 0)
        {
            await UnitOfWork.Workspaces.ReleaseStorage(job.WorkspaceId, reclaimedBytes, cancellationToken);
        }

        if (storageKey is not null)
        {
            await DeleteArtefact(storageKey, cancellationToken);
        }

        return ClientResponse.Success;
    }

    private async Task DeleteArtefact(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await Storage.DeleteFileAsync(storageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The row is already gone, so a failed blob delete must not fail the request.
        }
    }
}
