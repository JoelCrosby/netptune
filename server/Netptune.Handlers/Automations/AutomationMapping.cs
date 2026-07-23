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
                conditions = trigger.Conditions,
                conditionGroup = trigger.ConditionGroup,
                statusId = trigger.StatusId,
                assigneeChangeMode = trigger.AssigneeChangeMode,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskStatusChanged => JsonSerializer.SerializeToDocument(new
            {
                statusId = trigger.StatusId,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskUnassignedFor => JsonSerializer.SerializeToDocument(new
            {
                durationDays = trigger.DurationDays,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskDueDateApproaching => JsonSerializer.SerializeToDocument(new
            {
                durationDays = trigger.DurationDays,
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
            var conditions = JsonUtils.ReadList<AutomationFieldCondition>(config, "conditions");
            var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(config, "conditionGroup");
            var statusId = JsonUtils.ReadInt(config, "statusId");
            var assigneeChangeMode = JsonUtils.ReadEnum<AssigneeChangeMode>(config, "assigneeChangeMode");

            return new AutomationTriggerViewModel
            {
                Type = type,
                Fields = fields,
                Conditions = conditions,
                ConditionGroup = conditionGroup,
                StatusId = statusId,
                AssigneeChangeMode = assigneeChangeMode,
            };
        }

        if (type == AutomationTriggerType.TaskStatusChanged)
        {
            var statusId = JsonUtils.ReadInt(config, "statusId");

            return new AutomationTriggerViewModel
            {
                Type = type,
                Fields = [TaskChangeField.Status],
                Conditions = [],
                StatusId = statusId,
            };
        }

        var isDurationTrigger = type is
            AutomationTriggerType.TaskUnassignedFor or
            AutomationTriggerType.TaskDueDateApproaching;

        if (isDurationTrigger)
        {
            var durationDays = JsonUtils.ReadInt(config, "durationDays");

            return new AutomationTriggerViewModel
            {
                Type = type,
                DurationDays = durationDays,
            };
        }

        return new AutomationTriggerViewModel { Type = type };
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
