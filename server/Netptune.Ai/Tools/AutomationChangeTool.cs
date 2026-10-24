using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Automations;

namespace Netptune.Ai.Tools;

public sealed class AutomationChangeTool : IAiTool
{
    public const string ToolName = "propose_automation_change";

    private readonly AiToolActions Actions;

    public AutomationChangeTool(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        IAutomationActionRegistry actionRegistry)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateAutomationAction(mediator, changeSet, actionRegistry),
            ["update"] = new UpdateAutomationAction(mediator, changeSet, actionRegistry),
            ["set_enabled"] = new SetAutomationEnabledAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating or changing an automation rule, or turning one on or off. Call list_automations with "
        + "includeReference first for how rules are written, and read a rule with ruleId before updating it. "
        + "create takes a whole rule: name, trigger, actions and optionally a scope; executionUserId defaults to the "
        + "first enabled automation service account. A new rule is created disabled; pass isEnabled true only when "
        + "the user asked for it to be live. update replaces only the sections it is given: name, isEnabled, "
        + "executionUserId, the scope (any of projectId, boardId or sprintId replaces all three, so pass null for "
        + "none), trigger as a whole or actions as a whole list. set_enabled turns a rule on or off. Changing "
        + "anything but the name, or disabling a rule, cancels its pending delayed actions.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "set_enabled"] },
          "ruleId": { "type": "integer", "description": "Update and set_enabled: the rule, from list_automations." },
          "name": { "type": "string", "description": "The rule name." },
          "isEnabled": { "type": "boolean", "description": "Whether the rule runs. Required for set_enabled." },
          "executionUserId": { "type": "string", "description": "A userId from list_records automation_service_accounts." },
          "projectId": { "type": "integer", "description": "Scope the rule to one project." },
          "boardId": { "type": "integer", "description": "Scope the rule to one board." },
          "sprintId": { "type": "integer", "description": "Scope the rule to one sprint." },
          "trigger": { "type": "object", "description": "The whole trigger: type, its settings and an optional conditionGroup." },
          "actions": { "type": "array", "items": { "type": "object" }, "description": "Every action, in order." }
        }
        """,
        "action");

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return Actions.IsAvailable(permissions);
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        return Actions.GetRequiredPermissions(arguments);
    }

    public IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        return Actions.GetChangePermissions(changeName, payload);
    }

    public string DescribeCall(JsonElement arguments)
    {
        return Actions.DescribeCall(Name, arguments);
    }

    public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        return Actions.Execute(arguments, cancellationToken);
    }
}
