using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRunsQuery(int RuleId) : IRequest<ClientResponse<List<AutomationRunViewModel>>>;

public sealed class GetAutomationRunsQueryHandler
    : IRequestHandler<GetAutomationRunsQuery, ClientResponse<List<AutomationRunViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationRunsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<AutomationRunViewModel>>> Handle(
        GetAutomationRunsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.RuleId, workspaceId, true, cancellationToken);

        if (rule is null) return ClientResponse<List<AutomationRunViewModel>>.NotFound;

        var runs = await UnitOfWork.Automations.GetRuns(request.RuleId, workspaceId, cancellationToken: cancellationToken);

        return ClientResponse<List<AutomationRunViewModel>>.Success(runs);
    }
}
