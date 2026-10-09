using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRunSummaryQuery(int RuleId) : IRequest<ClientResponse<AutomationRunSummaryViewModel>>;

public sealed class GetAutomationRunSummaryQueryHandler
    : IRequestHandler<GetAutomationRunSummaryQuery, ClientResponse<AutomationRunSummaryViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationRunSummaryQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AutomationRunSummaryViewModel>> Handle(
        GetAutomationRunSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.RuleId, workspaceId, true, cancellationToken);

        if (rule is null)
        {
            return ClientResponse<AutomationRunSummaryViewModel>.NotFound;
        }

        var summary = await UnitOfWork.Automations.GetRunSummary(request.RuleId, workspaceId, cancellationToken);

        return ClientResponse<AutomationRunSummaryViewModel>.Success(summary);
    }
}
