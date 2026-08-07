using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record DeleteImportSessionCommand(Guid PublicId) : IRequest<ClientResponse>;

public sealed class DeleteImportSessionCommandHandler : IRequestHandler<DeleteImportSessionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IIdentityService Identity;
    private readonly IImportSourceStore Store;

    public DeleteImportSessionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportSourceStore store,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Store = store;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse> Handle(DeleteImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse.NotFound;
        }

        var isDeletable = ImportStages.CanDelete(session.Stage);

        if (!isDeletable)
        {
            return ClientResponse.Failed("An import that is running cannot be deleted.");
        }

        var storageKey = session.StorageKey;

        session.QuotaReleased = true;
        session.Delete(Identity.GetCurrentUserId());

        await UnitOfWork.CompleteAsync(cancellationToken);

        await DeleteSource(storageKey, cancellationToken);

        return ClientResponse.Success;
    }

    private async Task DeleteSource(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await Store.Delete(storageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The row is already gone, so a failed blob delete must not fail the request.
        }
    }
}
