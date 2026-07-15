using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Commands;

public sealed record DeleteWorkspaceFileCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteWorkspaceFileCommandHandler : IRequestHandler<DeleteWorkspaceFileCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public DeleteWorkspaceFileCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse> Handle(DeleteWorkspaceFileCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var entity = await UnitOfWork.WorkspaceFiles.GetInWorkspace(request.Id, workspaceId, isReadonly: true, cancellationToken);

        if (entity is null)
        {
            return ClientResponse.NotFound;
        }

        var released = await UnitOfWork.Transaction(async () =>
        {
            var marked = await UnitOfWork.WorkspaceFiles.TryMarkQuotaReleased(entity.Id, userId, cancellationToken);

            if (!marked)
            {
                return false;
            }

            await UnitOfWork.TaskFiles.DeleteByWorkspaceFileId(entity.Id, cancellationToken);
            await UnitOfWork.Workspaces.ReleaseStorage(entity.WorkspaceId, entity.SizeBytes, cancellationToken);

            return true;
        });

        if (released)
        {
            try
            {
                await Storage.DeleteFileAsync(entity.StorageKey, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The reconciliation job retries physical deletion.
            }
        }

        return ClientResponse.Success;
    }
}
