using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Queries;

public sealed record GetAutomationDryRunQuery(int RuleId, int TaskId)
    : IRequest<ClientResponse<AutomationDryRunViewModel>>;

public sealed class GetAutomationDryRunQueryHandler
    : IRequestHandler<GetAutomationDryRunQuery, ClientResponse<AutomationDryRunViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAutomationDryRunQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AutomationDryRunViewModel>> Handle(
        GetAutomationDryRunQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.RuleId, workspaceId, true, cancellationToken);

        if (rule is null)
        {
            return ClientResponse<AutomationDryRunViewModel>.NotFound;
        }

        var task = await UnitOfWork.Tasks.GetAutomationTask(request.TaskId, cancellationToken);
        var isTaskInWorkspace = task is not null && task.WorkspaceId == workspaceId;

        if (task is null || !isTaskInWorkspace)
        {
            return ClientResponse<AutomationDryRunViewModel>.NotFound;
        }

        var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(rule.TriggerConfig, "conditionGroup");
        var supportsChangeOperators = rule.TriggerType == AutomationTriggerType.TaskChanged;
        var explanation = conditionGroup?.Explain(task, null, supportsChangeOperators);

        var dryRun = new AutomationDryRunViewModel
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            IsEnabled = rule.IsEnabled,
            TriggerType = rule.TriggerType,
            TaskId = task.Id,
            TaskName = task.Name,
            ConditionsMatch = explanation?.IsMatch ?? true,
            HasUnevaluableConditions = HasUnevaluableConditions(explanation),
            ConditionGroup = explanation,
        };

        return ClientResponse<AutomationDryRunViewModel>.Success(dryRun);
    }

    private static bool HasUnevaluableConditions(AutomationConditionGroupExplanation? group)
    {
        if (group is null)
        {
            return false;
        }

        var hasUnevaluableCondition = group.Conditions.Any(condition => !condition.IsEvaluable);

        if (hasUnevaluableCondition)
        {
            return true;
        }

        return group.Groups.Any(HasUnevaluableConditions);
    }
}
