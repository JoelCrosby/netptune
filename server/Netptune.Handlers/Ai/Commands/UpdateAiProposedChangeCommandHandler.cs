using Mediator;

using Netptune.Core.Enums;
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

    public UpdateAiProposedChangeCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiUndoCatalog undoCatalog)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        UndoCatalog = undoCatalog;
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
            return ClientResponse<AiChangeSetViewModel>.Failed("Only a pending change set can be edited.");
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
            return ClientResponse<AiChangeSetViewModel>.Failed("This change has already run, so it cannot be edited.");
        }

        var edit = AiProposedChangeEditor.Apply(
            change.Summary,
            change.Fields,
            change.Payload,
            command.Request.Fields);

        if (!edit.IsSuccess)
        {
            return ClientResponse<AiChangeSetViewModel>.Failed(edit.Error!);
        }

        change.Summary = edit.Summary;
        change.Fields = edit.Fields;
        change.Payload = edit.Payload;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var taskIds = AiChangeSetMapper.CollectTaskIds(changes);
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);
        var model = AiChangeSetMapper.ToViewModel(changeSet, changes, tasks, UndoCatalog);

        return ClientResponse<AiChangeSetViewModel>.Success(model);
    }
}
