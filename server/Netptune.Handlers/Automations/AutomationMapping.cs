using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations;

internal static class AutomationMapping
{
    public static AutomationRuleViewModel ToViewModel(this AutomationRule rule, IAutomationActionRegistry actionRegistry)
    {
        return new AutomationRuleViewModel
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            ExecutionUserId = rule.ExecutionUserId,
            ProjectId = rule.ProjectId,
            BoardId = rule.BoardId,
            SprintId = rule.SprintId,
            Trigger = ReadTrigger(rule.TriggerType, rule.TriggerConfig),
            Actions = rule.Actions
                .Where(action => !action.IsDeleted)
                .OrderBy(action => action.SortOrder)
                .Select(action => ToViewModel(action, actionRegistry))
                .ToList(),
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
        };
    }

    public static JsonDocument? ToTriggerConfig(AutomationTriggerRequest trigger)
    {
        return trigger.Type switch
        {
            AutomationTriggerType.TaskChanged => JsonSerializer.SerializeToDocument(new
            {
                fields = trigger.Fields,
                conditionGroup = trigger.ConditionGroup,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskUnassignedFor or
            AutomationTriggerType.TaskDueDateApproaching or
            AutomationTriggerType.TaskInactiveFor or
            AutomationTriggerType.SprintEndingSoon => JsonSerializer.SerializeToDocument(new
            {
                durationDays = trigger.DurationDays,
                conditionGroup = trigger.ConditionGroup,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskCreated or
            AutomationTriggerType.TaskOverdue or
            AutomationTriggerType.TaskHasNoDueDate or
            AutomationTriggerType.SprintStarted or
            AutomationTriggerType.SprintCompleted or
            AutomationTriggerType.TaskBlocked or
            AutomationTriggerType.TaskUnblocked or
            AutomationTriggerType.SubtasksCompleted => JsonSerializer.SerializeToDocument(new
            {
                conditionGroup = trigger.ConditionGroup,
            }, JsonOptions.Default),
            _ => null,
        };
    }

    public static JsonDocument? ToActionConfig(AutomationActionRequest action, IAutomationActionRegistry actionRegistry)
    {
        return actionRegistry.Find(action.Type)?.CreateConfig(action);
    }

    public static AutomationTriggerViewModel ReadTrigger(AutomationTriggerType type, JsonDocument? config)
    {
        if (type == AutomationTriggerType.TaskChanged)
        {
            var fields = JsonUtils.ReadEnumList<TaskChangeField>(config, "fields");
            var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(config, "conditionGroup");

            return new AutomationTriggerViewModel
            {
                Type = type,
                Fields = fields,
                ConditionGroup = conditionGroup,
            };
        }

        var isDurationTrigger = type is
            AutomationTriggerType.TaskUnassignedFor or
            AutomationTriggerType.TaskDueDateApproaching or
            AutomationTriggerType.TaskInactiveFor or
            AutomationTriggerType.SprintEndingSoon;

        if (isDurationTrigger)
        {
            var durationDays = JsonUtils.ReadInt(config, "durationDays");
            var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(config, "conditionGroup");

            return new AutomationTriggerViewModel
            {
                Type = type,
                DurationDays = durationDays,
                ConditionGroup = conditionGroup,
            };
        }

        var scheduledConditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(config, "conditionGroup");

        return new AutomationTriggerViewModel
        {
            Type = type,
            ConditionGroup = scheduledConditionGroup,
        };
    }

    private static AutomationActionViewModel ToViewModel(AutomationAction action, IAutomationActionRegistry actionRegistry)
    {
        var automationAction = actionRegistry.Find(action.Type);

        if (automationAction is not null)
        {
            return automationAction.ToViewModel(action);
        }

        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
        };
    }
}
