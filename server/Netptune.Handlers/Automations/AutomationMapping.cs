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
    public static AutomationRuleViewModel ToViewModel(
        this AutomationRule rule,
        IAutomationActionRegistry actionRegistry)
    {
        return new AutomationRuleViewModel
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
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
        return type switch
        {
            AutomationTriggerType.TaskChanged => new AutomationTriggerViewModel
            {
                Type = type,
                Fields = ReadEnumList<TaskChangeField>(config, "fields"),
                Conditions = ReadList<AutomationFieldCondition>(config, "conditions"),
                StatusId = ReadInt(config, "statusId"),
                AssigneeChangeMode = ReadEnum<AssigneeChangeMode>(config, "assigneeChangeMode"),
            },
            AutomationTriggerType.TaskStatusChanged => new AutomationTriggerViewModel
            {
                Type = type,
                Fields = [TaskChangeField.Status],
                Conditions = [],
                StatusId = ReadInt(config, "statusId"),
            },
            AutomationTriggerType.TaskUnassignedFor => new AutomationTriggerViewModel
            {
                Type = type,
                DurationDays = ReadInt(config, "durationDays"),
            },
            AutomationTriggerType.TaskDueDateApproaching => new AutomationTriggerViewModel
            {
                Type = type,
                DurationDays = ReadInt(config, "durationDays"),
            },
            _ => new AutomationTriggerViewModel { Type = type },
        };
    }

    private static AutomationActionViewModel ToViewModel(
        AutomationAction action,
        IAutomationActionRegistry actionRegistry)
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

    private static TEnum? ReadEnum<TEnum>(JsonDocument? document, string property)
        where TEnum : struct, Enum
    {
        if (document is null || !document.RootElement.TryGetProperty(property, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intValue))
        {
            return Enum.IsDefined(typeof(TEnum), intValue) ? (TEnum)(object)intValue : null;
        }

        if (element.ValueKind == JsonValueKind.String && Enum.TryParse<TEnum>(element.GetString(), true, out var enumValue))
        {
            return enumValue;
        }

        return null;
    }

    private static List<TEnum> ReadEnumList<TEnum>(JsonDocument? document, string property)
        where TEnum : struct, Enum
    {
        if (document is null ||
            !document.RootElement.TryGetProperty(property, out var element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<TEnum>();

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number &&
                item.TryGetInt32(out var intValue) &&
                Enum.IsDefined(typeof(TEnum), intValue))
            {
                values.Add((TEnum)(object)intValue);
                continue;
            }

            if (item.ValueKind == JsonValueKind.String &&
                Enum.TryParse<TEnum>(item.GetString(), true, out var enumValue))
            {
                values.Add(enumValue);
            }
        }

        return values;
    }

    private static int? ReadInt(JsonDocument? document, string property)
    {
        return document is not null
               && document.RootElement.TryGetProperty(property, out var element)
               && element.ValueKind == JsonValueKind.Number
               && element.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static List<T> ReadList<T>(JsonDocument? document, string property)
    {
        if (document is null || !document.RootElement.TryGetProperty(property, out var element))
        {
            return [];
        }

        try
        {
            return element.Deserialize<List<T>>(JsonOptions.Default) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

}
