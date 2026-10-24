using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListAutomationsTool : IAiTool
{
    private const int DefaultPageSize = 25;

    private readonly IMediator Mediator;

    public ListAutomationsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_automations";

    public string Description =>
        "List the workspace's automation rules with what triggers each one, what it does, whether it is enabled, "
        + "and how its last run went. Use this to explain why a task changed on its own or what rules exist. "
        + "Pass ruleId to read one rule in full, with every trigger, condition and action setting; read a rule "
        + "this way before proposing a change to it. Pass includeReference once before writing or changing any "
        + "rule: it returns how triggers, conditions and actions are written, alongside the rule when ruleId is "
        + "also given and on its own otherwise.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Automations.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "search": { "type": "string", "description": "Optional name fragment to filter rules by." },
          "isEnabled": { "type": "boolean", "description": "Restrict to enabled or disabled rules." },
          "ruleId": { "type": "integer", "description": "Read this one rule in full instead of listing." },
          "includeReference": { "type": "boolean", "description": "Return the rule-writing reference." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var ruleId = AiToolSchema.GetInt(arguments, "ruleId");
        var includeReference = AiToolSchema.GetBool(arguments, "includeReference") == true;

        if (ruleId.HasValue)
        {
            return await ReadRule(ruleId.Value, includeReference, cancellationToken);
        }

        if (includeReference)
        {
            var reference = new JsonObject { ["reference"] = AiAutomationReference.Build() };

            return AiToolExecution.Success(reference.ToJsonString());
        }

        var filter = new AutomationRuleFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            IsEnabled = AiToolSchema.GetBool(arguments, "isEnabled"),
            Page = 1,
            PageSize = DefaultPageSize,
        };

        var result = await Mediator.Send(new GetAutomationRulesPagedQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "Automations could not be read.");
        }

        var rules = result.Payload.Items.Select(rule => new
        {
            id = rule.Id,
            name = rule.Name,
            isEnabled = rule.IsEnabled,
            autoDisabledReason = rule.AutoDisabledReason,
            trigger = rule.Trigger.Type.ToString(),
            actions = rule.Actions.Select(action => action.Type.ToString()),
            projectId = rule.ProjectId,
            boardId = rule.BoardId,
            sprintId = rule.SprintId,
            warnings = rule.Warnings.Select(warning => warning.Message),
            lastRun = rule.LastRun is null
                ? null
                : new
                {
                    status = rule.LastRun.Status.ToString(),
                    message = rule.LastRun.Message,
                    createdAt = rule.LastRun.CreatedAt,
                },
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(rules));
    }

    private async Task<AiToolExecution> ReadRule(int ruleId, bool includeReference, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAutomationRuleQuery(ruleId), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? $"Automation rule {ruleId} is not in this workspace.");
        }

        var rule = result.Payload;
        var detail = new JsonObject
        {
            ["rule"] = JsonNode.Parse(AiAutomationJson.ToRuleElement(rule).GetRawText()),
            ["autoDisabledReason"] = rule.AutoDisabledReason,
            ["warnings"] = new JsonArray(rule.Warnings.Select(warning => (JsonNode?)warning.Message).ToArray()),
        };

        if (includeReference)
        {
            detail["reference"] = AiAutomationReference.Build();
        }

        return AiToolExecution.Success(detail.ToJsonString());
    }
}
