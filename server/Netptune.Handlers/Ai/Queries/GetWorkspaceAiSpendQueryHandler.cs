using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceAiSpendQuery : IRequest<ClientResponse<AiSpendViewModel>>;

public sealed class GetWorkspaceAiSpendQueryHandler
    : IRequestHandler<GetWorkspaceAiSpendQuery, ClientResponse<AiSpendViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiSpendService Spend;

    public GetWorkspaceAiSpendQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiSpendService spend)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Spend = spend;
    }

    public async ValueTask<ClientResponse<AiSpendViewModel>> Handle(
        GetWorkspaceAiSpendQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);

        if (workspace is null)
        {
            return ClientResponse<AiSpendViewModel>.NotFound;
        }

        var summary = await Spend.GetSummary(workspace, cancellationToken);

        return ClientResponse<AiSpendViewModel>.Success(summary);
    }
}
