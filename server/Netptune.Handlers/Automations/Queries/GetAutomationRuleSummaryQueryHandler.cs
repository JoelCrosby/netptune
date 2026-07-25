using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRuleSummaryQuery
    : IRequest<ClientResponse<AutomationRuleSummaryViewModel>>;

public sealed class GetAutomationRuleSummaryQueryHandler
    : IRequestHandler<GetAutomationRuleSummaryQuery, ClientResponse<AutomationRuleSummaryViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationRuleSummaryQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AutomationRuleSummaryViewModel>> Handle(
        GetAutomationRuleSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var summary = await UnitOfWork.Automations.GetRuleSummary(workspaceId, cancellationToken);

        return ClientResponse<AutomationRuleSummaryViewModel>.Success(summary);
    }
}
