using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRulesQuery : IRequest<ClientResponse<List<AutomationRuleViewModel>>>;

public sealed class GetAutomationRulesQueryHandler
    : IRequestHandler<GetAutomationRulesQuery, ClientResponse<List<AutomationRuleViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationRulesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<AutomationRuleViewModel>>> Handle(
        GetAutomationRulesQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rules = await UnitOfWork.Automations.GetRulesInWorkspace(workspaceId, cancellationToken: cancellationToken);

        return ClientResponse<List<AutomationRuleViewModel>>.Success(rules.Select(rule => rule.ToViewModel()).ToList());
    }
}
