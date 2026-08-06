using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record UndoImportSessionCommand(Guid PublicId) : IRequest<ClientResponse<ImportUndoResult>>;

public sealed class UndoImportSessionCommandHandler : IRequestHandler<UndoImportSessionCommand, ClientResponse<ImportUndoResult>>
{
    private readonly IIdentityService Identity;
    private readonly IImportUndoService Undo;
    private readonly IImportSessionRepository ImportSessions;

    public UndoImportSessionCommandHandler(IIdentityService identity, IImportUndoService undo, IImportSessionRepository importSessions)
    {
        Identity = identity;
        Undo = undo;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportUndoResult>> Handle(UndoImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportUndoResult>.NotFound;
        }

        var isUndoable = ImportStages.CanUndo(session.Stage);

        if (!isUndoable)
        {
            return ClientResponse<ImportUndoResult>.Failed($"An import that is {session.Stage} cannot be undone.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            return ClientResponse<ImportUndoResult>.Failed("This import is too old to undo.");
        }

        var result = await Undo.Undo(session, Identity.GetCurrentUserId(), cancellationToken);

        return ClientResponse<ImportUndoResult>.Success(result);
    }
}
