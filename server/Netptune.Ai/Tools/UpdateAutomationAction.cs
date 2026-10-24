using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Automations;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateAutomationAction : IAiToolAction
{
    public const string Change = "propose_update_automation";

    private static readonly string[] ScopeArguments = ["projectId", "boardId", "sprintId"];

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly IAutomationActionRegistry ActionRegistry;

    public UpdateAutomationAction(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        IAutomationActionRegistry actionRegistry)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
        ActionRegistry = actionRegistry;
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
            NetptunePermissions.ServiceAccounts.Read,
            NetptunePermissions.Statuses.Read,
        };
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var ruleId = AiToolSchema.GetInt(arguments, "ruleId");

        if (!ruleId.HasValue)
        {
            return AiToolExecution.Failed("A ruleId is required.");
        }

        var result = await Mediator.Send(new GetAutomationRuleQuery(ruleId.Value), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed($"Automation rule {ruleId} is not in this workspace.");
        }

        var previous = AiAutomationJson.ToRequest(result.Payload);
        var merged = Merge(previous, arguments);

        if (merged.Error is not null)
        {
            return AiToolExecution.Failed(merged.Error);
        }

        var rule = AiAutomationJson.Normalize(merged.Value!, ActionRegistry);
        var isUnchanged = AiAutomationJson.AreSame(rule, previous);

        if (isUnchanged)
        {
            return AiToolExecution.Failed("No changes were supplied for this rule.");
        }

        var error = await AiAutomationProposal.Validate(Mediator, rule, cancellationToken);

        if (error is not null)
        {
            return AiToolExecution.Failed(error);
        }

        var isRenameOnly = AiAutomationJson.AreSame(rule, previous with { Name = rule.Name });
        var notice = isRenameOnly
            ? null
            : await AiAutomationProposal.CancellationNotice(Mediator, ruleId.Value, cancellationToken);
        var fields = await AiAutomationProposal.DescribeFields(Mediator, previous, rule, cancellationToken);
        var payload = JsonSerializer.SerializeToDocument(
            new { ruleId = ruleId.Value, rule, previous },
            AiAutomationJson.Options);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiAutomationProposal.EntityType,
            EntityId = ruleId.Value,
            Summary = Localized.Format(AppText.AiSummaryUpdateAutomation, previous.Name),
            Fields = fields,
            Payload = payload,
            ValidationStatus = AiChangeValidationStatus.Valid,
            ValidationMessage = notice,
        });

        return AiToolExecution.Success(
            $"Proposed updating automation {ruleId}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static AiAutomationRead<AutomationRuleRequest> Merge(AutomationRuleRequest previous, JsonElement arguments)
    {
        var rule = previous with
        {
            Name = AiToolSchema.GetString(arguments, "name")?.Trim() ?? previous.Name,
            IsEnabled = AiToolSchema.GetBool(arguments, "isEnabled") ?? previous.IsEnabled,
            ExecutionUserId = AiToolSchema.GetString(arguments, "executionUserId") ?? previous.ExecutionUserId,
        };

        var isRescoped = ScopeArguments.Any(name => arguments.TryGetProperty(name, out _));

        if (isRescoped)
        {
            rule = rule with
            {
                ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
                BoardId = AiToolSchema.GetInt(arguments, "boardId"),
                SprintId = AiToolSchema.GetInt(arguments, "sprintId"),
            };
        }

        var hasTrigger = arguments.TryGetProperty("trigger", out var triggerElement);

        if (hasTrigger)
        {
            var trigger = AiAutomationProposal.Read<AutomationTriggerRequest>(triggerElement, "trigger");

            if (trigger.Error is not null)
            {
                return AiAutomationRead<AutomationRuleRequest>.Failed(trigger.Error);
            }

            rule = rule with { Trigger = trigger.Value! };
        }

        var hasActions = arguments.TryGetProperty("actions", out var actionsElement);

        if (hasActions)
        {
            var actions = AiAutomationProposal.Read<List<AutomationActionRequest>>(actionsElement, "actions");

            if (actions.Error is not null)
            {
                return AiAutomationRead<AutomationRuleRequest>.Failed(actions.Error);
            }

            rule = rule with { Actions = actions.Value! };
        }

        return AiAutomationRead<AutomationRuleRequest>.Read(rule);
    }
}
