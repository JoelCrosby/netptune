using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Commands;

public sealed record UpdateAiProposedChangeCommand(Guid ChangeSetId, long ChangeId, UpdateAiProposedChangeRequest Request)
    : IRequest<ClientResponse<AiChangeSetViewModel>>;

public sealed class UpdateAiProposedChangeCommandHandler
    : IRequestHandler<UpdateAiProposedChangeCommand, ClientResponse<AiChangeSetViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiUndoCatalog UndoCatalog;
    private readonly IAiChangeEditCatalog EditCatalog;

    public UpdateAiProposedChangeCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiUndoCatalog undoCatalog,
        IAiChangeEditCatalog editCatalog)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        UndoCatalog = undoCatalog;
        EditCatalog = editCatalog;
    }

    public async ValueTask<ClientResponse<AiChangeSetViewModel>> Handle(
        UpdateAiProposedChangeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(
            command.ChangeSetId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return ClientResponse<AiChangeSetViewModel>.NotFound;
        }

        var isPending = changeSet.Status == AiChangeSetStatus.Pending;

        if (!isPending)
        {
            return ClientResponse<AiChangeSetViewModel>.Failed(AppText.ErrorOnlyPendingChangeSetCanEdited);
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var change = changes.FirstOrDefault(candidate => candidate.Id == command.ChangeId);

        if (change is null)
        {
            return ClientResponse<AiChangeSetViewModel>.NotFound;
        }

        var isUnapplied = change.ApplyStatus == AiChangeApplyStatus.Pending;

        if (!isUnapplied)
        {
            return ClientResponse<AiChangeSetViewModel>.Failed(AppText.ErrorChangeAlreadyRunCannotEdited);
        }

        var edit = await EditCatalog.Apply(change, changes, command.Request.Fields, cancellationToken);

        if (!edit.IsSuccess)
        {
            return ClientResponse<AiChangeSetViewModel>.Failed(edit.Error!);
        }

        change.Summary = edit.Summary;
        change.Fields = edit.Fields;
        change.Payload = edit.Payload;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var routeIds = await AiChangeRouteIdReader.Read(UnitOfWork, changes, cancellationToken);
        var model = AiChangeSetMapper.ToViewModel(changeSet, changes, routeIds, UndoCatalog, EditCatalog);

        return ClientResponse<AiChangeSetViewModel>.Success(model);
    }
}
