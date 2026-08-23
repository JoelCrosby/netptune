using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Models.Automations;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationRulesPagedQuery(AutomationRuleFilter Filter)
    : IRequest<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>;

public sealed class GetAutomationRulesPagedQueryHandler
    : IRequestHandler<GetAutomationRulesPagedQuery, ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public GetAutomationRulesPagedQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>> Handle(
        GetAutomationRulesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.Automations.GetRulesPaged(workspaceId, request.Filter, cancellationToken);
        var ruleIds = page.Items.Select(rule => rule.Id).ToList();
        var latestRuns = await UnitOfWork.Automations.GetLatestRuns(ruleIds, cancellationToken);

        var viewModels = page.Items.Select(rule => rule.ToViewModel(ActionRegistry)).ToList();
        var warnings = await AutomationRuleReferenceAnalyzer.Analyze(
            UnitOfWork,
            viewModels,
            workspaceId,
            cancellationToken);

        var items = viewModels
            .Select(rule => ToListItem(rule, latestRuns, warnings))
            .ToList();

        var result = new PagedResponse<AutomationRuleListItemViewModel>(
            items,
            page.Page,
            page.PageSize,
            page.TotalCount);

        return ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>.Success(result);
    }

    private static AutomationRuleListItemViewModel ToListItem(
        AutomationRuleViewModel rule,
        Dictionary<int, AutomationRunViewModel> latestRuns,
        Dictionary<int, List<AutomationRuleWarning>> warnings)
    {
        return new AutomationRuleListItemViewModel
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            AutoDisabledAt = rule.AutoDisabledAt,
            AutoDisabledReason = rule.AutoDisabledReason,
            ExecutionUserId = rule.ExecutionUserId,
            ProjectId = rule.ProjectId,
            BoardId = rule.BoardId,
            SprintId = rule.SprintId,
            Trigger = rule.Trigger,
            Actions = rule.Actions,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            LastRun = latestRuns.GetValueOrDefault(rule.Id),
            Warnings = warnings.GetValueOrDefault(rule.Id) ?? [],
        };
    }
}
