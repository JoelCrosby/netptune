using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateAutomationChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public static string EditedBeforeApplyMessage => AppText.AiChangeErrorRuleEditedAfterChangeProposedLeftNow;

    public static string EditedSinceMessage => AppText.AiChangeErrorRuleEditedSinceChangeAppliedLeftNow;

    private readonly IMediator Mediator;

    public UpdateAutomationChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => UpdateAutomationAction.Change;

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Automations.Manage,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var ruleId = AiChangePayload.ReadInt(payload, "ruleId") ?? change.EntityId;
        var rule = AiAutomationRuleReader.ReadRule(payload, "rule");
        var previous = AiAutomationRuleReader.ReadRule(payload, "previous");

        if (!ruleId.HasValue || rule is null || previous is null)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRuleChangeRefersCouldNotResolved);
        }

        var current = await AiAutomationRuleReader.ReadCurrent(Mediator, ruleId.Value, cancellationToken);

        if (current is null)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRuleNoLongerWorkspace);
        }

        var isAsProposed = AiAutomationJson.AreSame(current, previous);

        if (!isAsProposed)
        {
            return AiChangePayload.Failure(change, EditedBeforeApplyMessage);
        }

        var response = await Mediator.Send(new UpdateAutomationRuleCommand(ruleId.Value, rule), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotUpdated);
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

        var snapshot = new { ruleId = ruleId.Value, rule = current };

        return JsonSerializer.SerializeToDocument(snapshot, AiAutomationJson.Options);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = change.UndoPayload?.RootElement;

        if (!snapshot.HasValue)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var ruleId = AiChangePayload.ReadInt(snapshot.Value, "ruleId");
        var restored = AiAutomationRuleReader.ReadRule(snapshot.Value, "rule");

        if (!ruleId.HasValue || restored is null)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var current = await AiAutomationRuleReader.ReadCurrent(Mediator, ruleId.Value, cancellationToken);

        if (current is null)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorRuleNoLongerWorkspace);
        }

        var applied = AiAutomationRuleReader.ReadRule(change.Payload.RootElement, "rule");
        var isUnchangedSinceApply = applied is not null && AiAutomationJson.AreSame(current, applied);

        if (!isUnchangedSinceApply)
        {
            return AiChangeUndoResult.Failure(change, EditedSinceMessage);
        }

        var response = await Mediator.Send(new UpdateAutomationRuleCommand(ruleId.Value, restored), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotRestored);
        }

        return AiChangeUndoResult.Undone(change, ruleId.Value);
    }
}
