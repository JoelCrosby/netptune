using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetPendingAiChangeSetQuery(Guid ConversationId) : IRequest<ClientResponse<AiChangeSetViewModel>>;

public sealed class GetPendingAiChangeSetQueryHandler
    : IRequestHandler<GetPendingAiChangeSetQuery, ClientResponse<AiChangeSetViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPendingAiChangeSetQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiChangeSetViewModel>> Handle(
        GetPendingAiChangeSetQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSet = await UnitOfWork.AiChangeSets.GetPending(
            query.ConversationId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return ClientResponse<AiChangeSetViewModel>.NotFound;
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var model = await AiChangeSetMapper.ToViewModel(
            changeSet,
            changes,
            UnitOfWork.Tasks,
            cancellationToken);

        return ClientResponse<AiChangeSetViewModel>.Success(model);
    }
}
