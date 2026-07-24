using System.Text.Json;

using Netptune.Automation.Matching;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class ScheduledActionEligibilityEvaluator
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IAutomationActionRegistry ActionRegistry;

    public ScheduledActionEligibilityEvaluator(
        INetptuneUnitOfWork unitOfWork,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        ActionRegistry = actionRegistry;
    }

    internal async Task<ScheduledEligibility> Evaluate(
        ScheduledAutomationAction scheduledAction,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var rule = scheduledAction.AutomationRule;
        var action = scheduledAction.AutomationAction;
        var task = scheduledAction.Task;
        var hasEnabledRule = !rule.IsDeleted && rule.IsEnabled;
        var hasEnabledAction = !action.IsDeleted;
        var hasExpectedActionType = action.Type == scheduledAction.ActionType;
        var hasActiveTask = !task.IsDeleted;
        var hasExpectedStatus = task.StatusId == scheduledAction.ExpectedStatusId;

        var hasValidReferences = hasEnabledRule &&
            hasEnabledAction &&
            hasExpectedActionType &&
            hasActiveTask &&
            hasExpectedStatus;

        if (!hasValidReferences)
        {
            return ScheduledEligibility.Cancelled("The rule, action, or task no longer matches.");
        }

        var matches = rule.TriggerType switch
        {
            AutomationTriggerType.TaskChanged => MatchesTaskChangedRule(scheduledAction),
            AutomationTriggerType.TaskUnassignedFor => MatchesUnassignedRule(scheduledAction, now),
            AutomationTriggerType.TaskDueDateApproaching => MatchesDueDateRule(scheduledAction, now),
            AutomationTriggerType.TaskCreated => MatchesConditions(scheduledAction),
            AutomationTriggerType.TaskOverdue => MatchesOverdueRule(scheduledAction, now),
            AutomationTriggerType.TaskHasNoDueDate => MatchesNoDueDateRule(scheduledAction),
            AutomationTriggerType.TaskInactiveFor => MatchesInactiveRule(scheduledAction, now),
            _ => false,
        };

        if (!matches)
        {
            return ScheduledEligibility.Cancelled("The rule, action, or task no longer matches.");
        }

        if (rule.ExecutionUserId is null || rule.ExecutionUserId != scheduledAction.OwnerId)
        {
            return ScheduledEligibility.Failed("The automation rule has no valid execution principal.");
        }

        var principal = await UnitOfWork.ServiceAccounts.GetAutomationPrincipal(
            rule.ExecutionUserId,
            rule.WorkspaceId,
            cancellationToken);

        if (principal is null || !principal.IsEnabled)
        {
            return ScheduledEligibility.Failed("The automation execution principal is no longer available.");
        }

        var automationAction = ActionRegistry.Find(action.Type);
        var requiredPermissions = AutomationPermissionPolicy.GetRequiredPermissions([action], ActionRegistry);

        if (automationAction is null || !AutomationPermissionPolicy.HasRequiredPermissions(principal.Permissions, requiredPermissions))
        {
            return ScheduledEligibility.Failed("The automation execution principal no longer has the permission required by this action.");
        }

        return ScheduledEligibility.Eligible;
    }

    private static bool MatchesTaskChangedRule(ScheduledAutomationAction scheduledAction)
    {
        if (scheduledAction.TriggerContext is null)
        {
            return true;
        }

        var message = scheduledAction.TriggerContext.RootElement.Deserialize<TaskChangedMessage>(JsonOptions.Default);

        if (message is null)
        {
            return false;
        }

        return TaskChangedRuleConditions.Match(scheduledAction.AutomationRule, message, scheduledAction.Task);
    }

    private static bool MatchesUnassignedRule(ScheduledAutomationAction scheduledAction, DateTime now)
    {
        var durationDays = JsonUtils.ReadInt(scheduledAction.AutomationRule.TriggerConfig, "durationDays");
        var hasValidDuration = durationDays is >= 1;

        if (!hasValidDuration)
        {
            return false;
        }

        var task = scheduledAction.Task;
        var taskTimestamp = task.UpdatedAt ?? task.CreatedAt;
        var remainsUnassigned = task.ProjectTaskAppUsers.Count == 0;
        var duration = durationDays.GetValueOrDefault();
        var hasElapsed = taskTimestamp <= now.AddDays(-duration);

        var matchesConditions = MatchesConditions(scheduledAction);

        return remainsUnassigned && hasElapsed && matchesConditions;
    }

    private static bool MatchesDueDateRule(ScheduledAutomationAction scheduledAction, DateTime now)
    {
        var durationDays = JsonUtils.ReadInt(scheduledAction.AutomationRule.TriggerConfig, "durationDays");
        var hasValidDuration = durationDays is >= 0 and <= 365;

        if (!hasValidDuration)
        {
            return false;
        }

        var duration = durationDays.GetValueOrDefault();
        var expectedDueDate = DateOnly.FromDateTime(now).AddDays(duration);

        var matchesDueDate = scheduledAction.Task.DueDate == expectedDueDate;
        var matchesConditions = MatchesConditions(scheduledAction);

        return matchesDueDate && matchesConditions;
    }

    private static bool MatchesOverdueRule(ScheduledAutomationAction scheduledAction, DateTime now)
    {
        var task = scheduledAction.Task;
        var today = AutomationTimeZones.Today(scheduledAction.AutomationRule, now);
        var isOverdue = task.DueDate < today;
        var isIncomplete = task.Status.Category != StatusCategory.Done;
        var matchesConditions = MatchesConditions(scheduledAction);

        return isOverdue && isIncomplete && matchesConditions;
    }

    private static bool MatchesNoDueDateRule(ScheduledAutomationAction scheduledAction)
    {
        var task = scheduledAction.Task;
        var hasNoDueDate = task.DueDate is null;
        var isIncomplete = task.Status.Category != StatusCategory.Done;
        var matchesConditions = MatchesConditions(scheduledAction);

        return hasNoDueDate && isIncomplete && matchesConditions;
    }

    private static bool MatchesInactiveRule(ScheduledAutomationAction scheduledAction, DateTime now)
    {
        var durationDays = JsonUtils.ReadInt(scheduledAction.AutomationRule.TriggerConfig, "durationDays");
        var hasValidDuration = durationDays is >= 1 and <= 365;

        if (!hasValidDuration)
        {
            return false;
        }

        var task = scheduledAction.Task;
        var lastActivityAt = task.UpdatedAt ?? task.CreatedAt;
        var hasReachedDuration = lastActivityAt <= now.AddDays(-durationDays!.Value);
        var matchesConditions = MatchesConditions(scheduledAction);

        return hasReachedDuration && matchesConditions;
    }

    private static bool MatchesConditions(ScheduledAutomationAction scheduledAction)
    {
        return AutomationRuleConditions.Match(scheduledAction.AutomationRule, scheduledAction.Task);
    }
}

internal sealed record ScheduledEligibility(bool CanExecute, ScheduledAutomationActionStatus Status, string? Message)
{
    public static readonly ScheduledEligibility Eligible = new(true, ScheduledAutomationActionStatus.Processing, null);

    public static ScheduledEligibility Cancelled(string message)
    {
        return new ScheduledEligibility(false, ScheduledAutomationActionStatus.Cancelled, message);
    }

    public static ScheduledEligibility Failed(string message)
    {
        return new ScheduledEligibility(false, ScheduledAutomationActionStatus.Failed, message);
    }
}
