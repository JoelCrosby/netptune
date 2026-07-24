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
                conditionGroup = trigger.ConditionGroup,
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
