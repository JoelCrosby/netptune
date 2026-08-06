using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Messages;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record CommitImportSessionCommand(Guid PublicId, bool SkipFailingRows) : IRequest<ClientResponse<ImportSessionViewModel>>;

public sealed class CommitImportSessionCommandHandler : IRequestHandler<CommitImportSessionCommand, ClientResponse<ImportSessionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;
    private readonly IImportSessionRepository ImportSessions;

    public CommitImportSessionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventPublisher eventPublisher,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventPublisher = eventPublisher;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSessionViewModel>> Handle(CommitImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        if (session.Mapping is null)
        {
            return ClientResponse<ImportSessionViewModel>.Failed("Map the file before committing it.");
        }

        var isCommittable = ImportStages.CanCommit(session.Stage);

        if (!isCommittable)
        {
            return ClientResponse<ImportSessionViewModel>.Failed(
                $"An import that is {session.Stage} cannot be committed.");
        }

        session.Stage = ImportStage.Committing;
        session.ProgressPercent = 0;
        session.ProgressMessage = "Queued";
        session.Error = null;

        await UnitOfWork.CompleteAsync(cancellationToken);

        await EventPublisher.Dispatch(new ImportCommitRequestedMessage
        {
            WorkspaceId = workspaceId,
            ImportSessionId = session.Id,
            UserId = Identity.GetCurrentUserId(),
            SkipFailingRows = request.SkipFailingRows,
        });

        var viewModel = await ImportSessions.GetViewModel(session.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ImportSessionViewModel>.NotFound;
        }

        return ClientResponse<ImportSessionViewModel>.Success(viewModel);
    }
}
