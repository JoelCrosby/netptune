using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Netptune.Automation.Rules;
using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
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

    public static AutomationRuleRequest ToRequest(AutomationRuleViewModel rule)
    {
        var element = ToRuleElement(rule);

        return element.Deserialize<AutomationRuleRequest>(Options)!;
    }

    // Shapes a request the way the rule will read back once saved, so a proposal can be compared with the
    // rule it is about to replace or has replaced.
    public static AutomationRuleRequest Normalize(
        AutomationRuleRequest request,
        IAutomationActionRegistry actionRegistry)
    {
        var triggerConfig = AutomationMapping.ToTriggerConfig(request.Trigger);
        var storedTrigger = AutomationMapping.ReadTrigger(request.Trigger.Type, triggerConfig);
        var actions = request.Actions
            .Select(action => NormalizeAction(action, actionRegistry))
            .ToList();

        return request with
        {
            Name = request.Name.Trim(),
            Trigger = Convert<AutomationTriggerRequest>(storedTrigger),
            Actions = actions,
        };
    }

    public static bool AreSame(AutomationRuleRequest first, AutomationRuleRequest second)
    {
        var firstNode = JsonSerializer.SerializeToNode(first, Options);
        var secondNode = JsonSerializer.SerializeToNode(second, Options);

        return JsonNode.DeepEquals(firstNode, secondNode);
    }

    private static AutomationActionRequest NormalizeAction(
        AutomationActionRequest action,
        IAutomationActionRegistry actionRegistry)
    {
        var automationAction = actionRegistry.Find(action.Type);

        if (automationAction is null)
        {
            return action;
        }

        var stored = new AutomationAction
        {
            Type = action.Type,
            Config = AutomationMapping.ToActionConfig(action, actionRegistry),
        };
        var readBack = automationAction.ToViewModel(stored);

        return Convert<AutomationActionRequest>(readBack);
    }

    private static T Convert<T>(object value)
    {
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), Options);

        return element.Deserialize<T>(Options)!;
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
