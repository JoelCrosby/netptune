using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateAutomationChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public static string EditedSinceMessage => AppText.AiChangeErrorRuleEditedSinceCreatedLeftPlace;

    private readonly IMediator Mediator;

    public CreateAutomationChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => CreateAutomationAction.Change;

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Automations.Manage,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var rule = AiAutomationRuleReader.ReadRule(change.Payload.RootElement, "rule");

        if (rule is null)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRuleMissingChange);
        }

        var response = await Mediator.Send(new CreateAutomationRuleCommand(rule), cancellationToken);

        if (!response.IsSuccess || response.Payload is null)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotCreated);
        }

        return AiChangePayload.Applied(change, response.Payload.Id);
    }

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var ruleId = change.AppliedEntityId;

        if (!ruleId.HasValue)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorCreatedRuleCouldNotResolved);
        }

        var current = await AiAutomationRuleReader.ReadCurrent(Mediator, ruleId.Value, cancellationToken);

        if (current is null)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorRuleNoLongerWorkspace);
        }

        var created = AiAutomationRuleReader.ReadRule(change.Payload.RootElement, "rule");
        var isUnchanged = created is not null && AiAutomationJson.AreSame(current, created);

        if (!isUnchanged)
        {
            return AiChangeUndoResult.Failure(change, EditedSinceMessage);
        }

        var response = await Mediator.Send(new DeleteAutomationRuleCommand(ruleId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? AppText.AiChangeErrorRuleCouldNotDeleted);
        }

        return AiChangeUndoResult.Undone(change, ruleId.Value);
    }
}
