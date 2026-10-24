using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Ai.Tools;

public sealed class SetAutomationEnabledAction : IAiToolAction
{
    public const string Change = "propose_set_automation_enabled";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public SetAutomationEnabledAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Automations.Manage };

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        return new HashSet<string>(StringComparer.Ordinal)
        {
            NetptunePermissions.Automations.Manage,
            NetptunePermissions.Automations.Read,
        };
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var ruleId = AiToolSchema.GetInt(arguments, "ruleId");
        var isEnabled = AiToolSchema.GetBool(arguments, "isEnabled");

        if (!ruleId.HasValue || !isEnabled.HasValue)
        {
            return AiToolExecution.Failed("A ruleId and isEnabled are required.");
        }

        var result = await Mediator.Send(new GetAutomationRuleQuery(ruleId.Value), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed($"Automation rule {ruleId} is not in this workspace.");
        }

        var rule = result.Payload;
        var state = isEnabled.Value ? "enabled" : "disabled";

        var isAlreadySet = rule.IsEnabled == isEnabled.Value;

        if (isAlreadySet)
        {
            return AiToolExecution.Failed($"Automation “{rule.Name}” is already {state}.");
        }

        var notice = isEnabled.Value
            ? null
            : await AiAutomationProposal.CancellationNotice(Mediator, rule.Id, cancellationToken);
        var verb = isEnabled.Value ? "Enable" : "Disable";
        var before = rule.IsEnabled ? "Enabled" : "Disabled";
        var after = isEnabled.Value ? "Enabled" : "Disabled";

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiAutomationProposal.EntityType,
            EntityId = rule.Id,
            Summary = $"{verb} automation “{rule.Name}”",
            Fields = [AiChangeFields.Text("enabled", before, after)],
            Payload = JsonSerializer.SerializeToDocument(new { ruleId = rule.Id, isEnabled = isEnabled.Value }),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ValidationMessage = notice,
        });

        return AiToolExecution.Success(
            $"Proposed setting automation {rule.Id} {state}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
