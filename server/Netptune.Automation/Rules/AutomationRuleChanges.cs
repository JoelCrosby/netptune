using System.Text.Json;
using System.Text.Json.Nodes;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Rules;

public static class AutomationRuleChanges
{
    public const string Name = "name";
    public const string Enabled = "enabled";
    public const string Scope = "scope";
    public const string RunsAs = "runsAs";
    public const string Trigger = "trigger";
    public const string Conditions = "conditions";
    public const string Actions = "actions";

    private const string ConditionGroupKey = "conditionGroup";

    public static List<string> Find(
        AutomationRule rule,
        AutomationRuleRequest request,
        IAutomationActionRegistry actionRegistry)
    {
        var changes = new List<string>();
        var requestedTriggerConfig = AutomationMapping.ToTriggerConfig(request.Trigger);
        var storedTrigger = ToObject(rule.TriggerConfig);
        var requestedTrigger = ToObject(requestedTriggerConfig);
        var storedConditions = storedTrigger[ConditionGroupKey];
        var requestedConditions = requestedTrigger[ConditionGroupKey];

        storedTrigger.Remove(ConditionGroupKey);
        requestedTrigger.Remove(ConditionGroupKey);

        var isRenamed = rule.Name != request.Name.Trim();
        var isEnabledChanged = rule.IsEnabled != request.IsEnabled;
        var isRescoped = rule.ProjectId != request.ProjectId
            || rule.BoardId != request.BoardId
            || rule.SprintId != request.SprintId;
        var isRunsAsChanged = rule.ExecutionUserId != request.ExecutionUserId;
        var isTriggerTypeChanged = rule.TriggerType != request.Trigger.Type;
        var isTriggerConfigChanged = !JsonNode.DeepEquals(storedTrigger, requestedTrigger);
        var isTriggerChanged = isTriggerTypeChanged || isTriggerConfigChanged;
        var isConditionsChanged = !JsonNode.DeepEquals(storedConditions, requestedConditions);
        var isActionsChanged = HaveActionsChanged(rule, request, actionRegistry);

        AddWhen(changes, isRenamed, Name);
        AddWhen(changes, isEnabledChanged, Enabled);
        AddWhen(changes, isRescoped, Scope);
        AddWhen(changes, isRunsAsChanged, RunsAs);
        AddWhen(changes, isTriggerChanged, Trigger);
        AddWhen(changes, isConditionsChanged, Conditions);
        AddWhen(changes, isActionsChanged, Actions);

        return changes;
    }

    private static bool HaveActionsChanged(
        AutomationRule rule,
        AutomationRuleRequest request,
        IAutomationActionRegistry actionRegistry)
    {
        var storedActions = rule.Actions
            .Where(action => !action.IsDeleted)
            .OrderBy(action => action.SortOrder)
            .Select(action => new ActionShape(action.Type, ToNode(action.Config)))
            .ToList();
        var requestedActions = request.Actions
            .Select(action => ToRequestedShape(action, actionRegistry))
            .ToList();
        var hasSameCount = storedActions.Count == requestedActions.Count;

        if (!hasSameCount)
        {
            return true;
        }

        var hasSameActions = storedActions
            .Zip(requestedActions)
            .All(pair => IsSameAction(pair.First, pair.Second));

        return !hasSameActions;
    }

    private static ActionShape ToRequestedShape(
        AutomationActionRequest action,
        IAutomationActionRegistry actionRegistry)
    {
        var config = AutomationMapping.ToActionConfig(action, actionRegistry);

        return new ActionShape(action.Type, ToNode(config));
    }

    private static bool IsSameAction(ActionShape stored, ActionShape requested)
    {
        var isSameType = stored.Type == requested.Type;
        var isSameConfig = JsonNode.DeepEquals(stored.Config, requested.Config);

        return isSameType && isSameConfig;
    }

    private static void AddWhen(List<string> changes, bool isChanged, string section)
    {
        if (isChanged)
        {
            changes.Add(section);
        }
    }

    private static JsonObject ToObject(JsonDocument? config)
    {
        return ToNode(config) as JsonObject ?? new JsonObject();
    }

    private static JsonNode? ToNode(JsonDocument? config)
    {
        return config is null ? null : JsonNode.Parse(config.RootElement.GetRawText());
    }

    private sealed record ActionShape(AutomationActionType Type, JsonNode? Config);
}
