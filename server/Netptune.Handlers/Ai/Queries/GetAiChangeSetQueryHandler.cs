using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiChangeSetQuery(Guid ChangeSetId) : IRequest<ClientResponse<AiChangeSetViewModel>>;

public sealed class GetAiChangeSetQueryHandler
    : IRequestHandler<GetAiChangeSetQuery, ClientResponse<AiChangeSetViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiUndoCatalog UndoCatalog;

    public GetAiChangeSetQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiUndoCatalog undoCatalog)
    {
        UndoCatalog = undoCatalog;
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiChangeSetViewModel>> Handle(
        GetAiChangeSetQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(
            query.ChangeSetId,
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
            UndoCatalog,
            cancellationToken);

        return ClientResponse<AiChangeSetViewModel>.Success(model);
    }
}
