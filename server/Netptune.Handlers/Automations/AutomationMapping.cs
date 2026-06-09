using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations;

internal static class AutomationMapping
{
    public static AutomationRuleViewModel ToViewModel(this AutomationRule rule)
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
                .Select(ToViewModel)
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
                status = trigger.Status,
                assigneeChangeMode = trigger.AssigneeChangeMode,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskStatusChanged => JsonSerializer.SerializeToDocument(new
            {
                status = trigger.Status,
            }, JsonOptions.Default),
            AutomationTriggerType.TaskUnassignedFor => JsonSerializer.SerializeToDocument(new
            {
                durationDays = trigger.DurationDays,
            }, JsonOptions.Default),
            _ => null,
        };
    }

    public static JsonDocument? ToActionConfig(AutomationActionRequest action)
    {
        return action.Type switch
        {
            AutomationActionType.NotifyTaskAssignees => JsonSerializer.SerializeToDocument(new
            {
                message = action.Message,
            }, JsonOptions.Default),
            AutomationActionType.FlagTask => JsonSerializer.SerializeToDocument(new
            {
                flagName = action.FlagName,
                flagDescription = action.FlagDescription,
            }, JsonOptions.Default),
            _ => null,
        };
    }

    public static AutomationTriggerViewModel ReadTrigger(AutomationTriggerType type, JsonDocument? config)
    {
        return type switch
        {
            AutomationTriggerType.TaskChanged => new AutomationTriggerViewModel
            {
                Type = type,
                Fields = ReadEnumList<TaskChangeField>(config, "fields"),
                Status = ReadEnum<ProjectTaskStatus>(config, "status"),
                AssigneeChangeMode = ReadEnum<AssigneeChangeMode>(config, "assigneeChangeMode"),
            },
            AutomationTriggerType.TaskStatusChanged => new AutomationTriggerViewModel
            {
                Type = type,
                Fields = [TaskChangeField.Status],
                Status = ReadEnum<ProjectTaskStatus>(config, "status"),
            },
            AutomationTriggerType.TaskUnassignedFor => new AutomationTriggerViewModel
            {
                Type = type,
                DurationDays = ReadInt(config, "durationDays"),
            },
            _ => new AutomationTriggerViewModel { Type = type },
        };
    }

    public static AutomationActionViewModel ToViewModel(this AutomationAction action)
    {
        return action.Type switch
        {
            AutomationActionType.NotifyTaskAssignees => new AutomationActionViewModel
            {
                Id = action.Id,
                Type = action.Type,
                SortOrder = action.SortOrder,
                Message = ReadString(action.Config, "message"),
            },
            AutomationActionType.FlagTask => new AutomationActionViewModel
            {
                Id = action.Id,
                Type = action.Type,
                SortOrder = action.SortOrder,
                FlagName = ReadString(action.Config, "flagName"),
                FlagDescription = ReadString(action.Config, "flagDescription"),
            },
            _ => new AutomationActionViewModel
            {
                Id = action.Id,
                Type = action.Type,
                SortOrder = action.SortOrder,
            },
        };
    }

    public static ProjectTaskStatus? ReadStatus(JsonDocument? config)
    {
        return ReadEnum<ProjectTaskStatus>(config, "status");
    }

    public static int? ReadDurationDays(JsonDocument? config)
    {
        return ReadInt(config, "durationDays");
    }

    public static string? ReadMessage(JsonDocument? config)
    {
        return ReadString(config, "message");
    }

    public static string? ReadFlagName(JsonDocument? config)
    {
        return ReadString(config, "flagName");
    }

    public static string? ReadFlagDescription(JsonDocument? config)
    {
        return ReadString(config, "flagDescription");
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
               && element.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static string? ReadString(JsonDocument? document, string property)
    {
        return document is not null
               && document.RootElement.TryGetProperty(property, out var element)
               && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}
