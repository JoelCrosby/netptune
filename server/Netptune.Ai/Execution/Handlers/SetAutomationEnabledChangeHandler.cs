using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class SetAutomationEnabledChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public static string ChangedSinceMessage => AppText.AiChangeErrorRuleTurnedOffSinceChangeAppliedLeft;

    private readonly IMediator Mediator;

    public SetAutomationEnabledChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => SetAutomationEnabledAction.Change;

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Automations.Manage,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var ruleId = AiChangePayload.ReadInt(payload, "ruleId") ?? change.EntityId;
        var isEnabled = AiChangePayload.ReadBool(payload, "isEnabled");

        if (!ruleId.HasValue || !isEnabled.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRuleChangeRefersCouldNotResolved);
        }

        var command = new SetAutomationRuleEnabledCommand(ruleId.Value, isEnabled.Value);
        var response = await Mediator.Send(command, cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotTurnedOff);
        }

        return AiChangePayload.Applied(change, ruleId.Value);
    }

    public async Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var ruleId = AiChangePayload.ReadInt(change.Payload.RootElement, "ruleId") ?? change.EntityId;

        if (!ruleId.HasValue)
        {
            return null;
        }

        var current = await AiAutomationRuleReader.ReadCurrent(Mediator, ruleId.Value, cancellationToken);

        if (current is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(new { ruleId = ruleId.Value, isEnabled = current.IsEnabled });
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = change.UndoPayload?.RootElement;

        if (!snapshot.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var ruleId = AiChangePayload.ReadInt(snapshot.Value, "ruleId");
        var wasEnabled = AiChangePayload.ReadBool(snapshot.Value, "isEnabled");
        var appliedEnabled = AiChangePayload.ReadBool(change.Payload.RootElement, "isEnabled");

        if (!ruleId.HasValue || !wasEnabled.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var current = await AiAutomationRuleReader.ReadCurrent(Mediator, ruleId.Value, cancellationToken);

        if (current is null)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRuleNoLongerWorkspace);
        }

        var isUnchangedSinceApply = current.IsEnabled == appliedEnabled;

        if (!isUnchangedSinceApply)
        {
            return AiChangePayload.Failure(change, ChangedSinceMessage);
        }

        var command = new SetAutomationRuleEnabledCommand(ruleId.Value, wasEnabled.Value);
        var response = await Mediator.Send(command, cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotTurnedBack);
        }

        return AiChangePayload.Applied(change, ruleId.Value);
    }
}
