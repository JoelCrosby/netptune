using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
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
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IAutomationTriggerEvaluator TriggerEvaluator;
    private readonly ILogger<GetAutomationDryRunQueryHandler> Logger;

    public GetAutomationDryRunQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry,
        IAutomationTriggerEvaluator triggerEvaluator,
        ILogger<GetAutomationDryRunQueryHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
        TriggerEvaluator = triggerEvaluator;
        Logger = logger;
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
        var trigger = TriggerEvaluator.Evaluate(rule, task, DateTime.UtcNow);

        var dryRun = new AutomationDryRunViewModel
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            IsEnabled = rule.IsEnabled,
            TriggerType = rule.TriggerType,
            TaskId = task.Id,
            TaskName = task.Name,
            ScopeMatches = AutomationRuleScope.Contains(rule, task),
            TriggerMatches = trigger.IsMatch,
            TriggerIsEvaluable = trigger.IsEvaluable,
            ConditionsMatch = explanation?.IsMatch ?? true,
            HasUnevaluableConditions = HasUnevaluableConditions(explanation),
            ConditionGroup = explanation,
            Actions = PlanActions(rule, task),
        };

        return ClientResponse<AutomationDryRunViewModel>.Success(dryRun);
    }

    private List<AutomationDryRunActionViewModel> PlanActions(AutomationRule rule, ProjectTask task)
    {
        var actions = rule.Actions
            .Where(action => !action.IsDeleted)
            .OrderBy(action => action.SortOrder)
            .ThenBy(action => action.Id)
            .ToList();

        return actions.Select(action => PlanAction(rule, task, action)).ToList();
    }

    private AutomationDryRunActionViewModel PlanAction(AutomationRule rule, ProjectTask task, AutomationAction action)
    {
        var handler = ActionRegistry.Find(action.Type);

        if (handler is null)
        {
            return new AutomationDryRunActionViewModel
            {
                ActionId = action.Id,
                Type = action.Type,
            };
        }

        var context = new AutomationActionPlanningContext
        {
            Rule = rule,
            Action = action,
            Task = task,
            ActorUserId = rule.ExecutionUserId ?? string.Empty,
        };

        try
        {
            var contribution = handler.Plan(context);

            return ToViewModel(action, contribution);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Automation dry run failed to plan action {ActionId}", action.Id);

            return new AutomationDryRunActionViewModel
            {
                ActionId = action.Id,
                Type = action.Type,
            };
        }
    }

    private static AutomationDryRunActionViewModel ToViewModel(
        AutomationAction action,
        AutomationActionPlanContribution contribution)
    {
        var notification = contribution.Notification;
        var flag = contribution.Flag;
        var deletion = contribution.TaskDeletion;
        var creation = contribution.TaskCreation;
        var relation = contribution.Relation;
        var updatedFields = DescribeTaskUpdate(contribution.TaskUpdate);

        var hasEffect = notification is not null
            || flag is not null
            || deletion is not null
            || contribution.CommentBody is not null
            || creation is not null
            || relation is not null
            || updatedFields.Count > 0;

        return new AutomationDryRunActionViewModel
        {
            ActionId = action.Id,
            Type = action.Type,
            HasEffect = hasEffect,
            Message = notification?.Message,
            RecipientUserIds = notification?.RecipientUserIds ?? [],
            IncludeProjectMembers = notification?.IncludeProjectMembers ?? false,
            RecipientRoles = notification?.RecipientRoles ?? [],
            Comment = contribution.CommentBody,
            FlagName = flag?.Name,
            UpdatedFields = updatedFields,
            CreatedTaskName = creation?.Name,
            RelationOperation = relation?.Operation,
            RelationTypeId = relation?.RelationTypeId,
            RelatedTaskId = relation?.RelatedTaskId,
            DelayMinutes = deletion is null ? null : (int) deletion.Delay.TotalMinutes,
        };
    }

    private static List<string> DescribeTaskUpdate(AutomationTaskUpdateContribution? update)
    {
        if (update is null)
        {
            return [];
        }

        var updatesDescription = update.Description is not null || update.ClearDescription;
        var updatesOwner = update.OwnerId is not null || update.ClearOwner;
        var updatesTags = update.AddTags.Count > 0 || update.RemoveTags.Count > 0;
        var updatesEstimate = update.EstimateType.HasValue || update.EstimateValue.HasValue || update.ClearEstimate;
        var updatesSprint = update.SprintId.HasValue || update.ClearSprint;

        List<(bool IsUpdated, string Label)> fields =
        [
            (update.StatusId.HasValue, "Status"),
            (update.Priority.HasValue, "Priority"),
            (update.Name is not null, "Name"),
            (updatesDescription, "Description"),
            (updatesOwner, "Owner"),
            (update.AssigneeIds is not null, "Assignees"),
            (updatesTags, "Tags"),
            (update.StartDate is not null, "Start date"),
            (update.DueDate is not null, "Due date"),
            (updatesEstimate, "Estimate"),
            (updatesSprint, "Sprint"),
            (update.BoardGroupId.HasValue, "Board group"),
        ];

        return fields
            .Where(field => field.IsUpdated)
            .Select(field => field.Label)
            .ToList();
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
