using System.Text.Json;

using Netptune.Automation.Matching;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

namespace Netptune.Automation.Execution;

internal sealed class ScheduledActionEligibilityEvaluator
{
    private readonly TaskChangedRuleMatcher TaskChangedMatcher;

    public ScheduledActionEligibilityEvaluator(TaskChangedRuleMatcher taskChangedMatcher)
    {
        TaskChangedMatcher = taskChangedMatcher;
    }

    internal bool IsEligible(ScheduledAutomationAction scheduledAction, DateTime now)
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
            return false;
        }

        return rule.TriggerType switch
        {
            AutomationTriggerType.TaskChanged or AutomationTriggerType.TaskStatusChanged =>
                MatchesTaskChangedRule(scheduledAction),
            AutomationTriggerType.TaskUnassignedFor => MatchesUnassignedRule(scheduledAction, now),
            AutomationTriggerType.TaskDueDateApproaching => MatchesDueDateRule(scheduledAction, now),
            _ => false,
        };
    }

    private bool MatchesTaskChangedRule(ScheduledAutomationAction scheduledAction)
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

        return TaskChangedMatcher.Matches(scheduledAction.AutomationRule, message, scheduledAction.Task);
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

        return remainsUnassigned && hasElapsed;
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

        return scheduledAction.Task.DueDate == expectedDueDate;
    }
}
