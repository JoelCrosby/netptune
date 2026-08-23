using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRuleQuery(int Id) : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class GetAutomationRuleQueryHandler
    : IRequestHandler<GetAutomationRuleQuery, ClientResponse<AutomationRuleViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public GetAutomationRuleQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<ClientResponse<AutomationRuleViewModel>> Handle(
        GetAutomationRuleQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.Id, workspaceId, true, cancellationToken);

        if (rule is null)
        {
            return ClientResponse<AutomationRuleViewModel>.NotFound;
        }

        var viewModel = rule.ToViewModel(ActionRegistry);
        var warnings = await AutomationRuleReferenceAnalyzer.Analyze(
            UnitOfWork,
            [viewModel],
            workspaceId,
            cancellationToken);

        var ruleWithWarnings = viewModel with
        {
            Warnings = warnings.GetValueOrDefault(viewModel.Id) ?? [],
        };

        return ClientResponse<AutomationRuleViewModel>.Success(ruleWithWarnings);
    }
}
