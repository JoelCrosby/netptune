using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Netptune.Core.Models.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Ai.Tools;

public static class AiAutomationJson
{
    // The model reads and writes rules in this shape, so empty lists and an action's false flags are left out
    // to save tokens; both are what an omitted property deserializes to.
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { SkipFalseAndEmpty },
        },
    };

    public static JsonElement ToRuleElement(AutomationRuleViewModel rule)
    {
        var document = new AiAutomationRuleDocument
        {
            Id = rule.Id,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            ExecutionUserId = rule.ExecutionUserId,
            ProjectId = rule.ProjectId,
            BoardId = rule.BoardId,
            SprintId = rule.SprintId,
            Trigger = rule.Trigger,
            Actions = rule.Actions.Cast<AutomationActionFields>().ToList(),
        };

        return JsonSerializer.SerializeToElement(document, Options);
    }

    private static void SkipFalseAndEmpty(JsonTypeInfo typeInfo)
    {
        var isActionType = typeof(AutomationActionFields).IsAssignableFrom(typeInfo.Type);

        foreach (var property in typeInfo.Properties)
        {
            var isFlag = isActionType && property.PropertyType == typeof(bool);
            var isList = property.PropertyType != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);

            if (isFlag)
            {
                property.ShouldSerialize = (_, value) => value is true;
            }
            else if (isList)
            {
                property.ShouldSerialize = (_, value) => value is IEnumerable items && items.GetEnumerator().MoveNext();
            }
        }
    }

    private sealed record AiAutomationRuleDocument
    {
        public int Id { get; init; }

        public required string Name { get; init; }

        public bool IsEnabled { get; init; }

        public string? ExecutionUserId { get; init; }

        public int? ProjectId { get; init; }

        public int? BoardId { get; init; }

        public int? SprintId { get; init; }

        public required AutomationTriggerViewModel Trigger { get; init; }

        public required List<AutomationActionFields> Actions { get; init; }
    }
}
