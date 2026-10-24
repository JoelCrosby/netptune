using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Automations;
using Netptune.Handlers.ServiceAccounts.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateAutomationAction : IAiToolAction
{
    public const string Change = "propose_create_automation";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly IAutomationActionRegistry ActionRegistry;

    public CreateAutomationAction(
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
            NetptunePermissions.ServiceAccounts.Read,
            NetptunePermissions.Statuses.Read,
        };
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var read = AiAutomationProposal.Read<AutomationRuleRequest>(arguments, "rule");

        if (read.Error is not null)
        {
            return AiToolExecution.Failed(read.Error);
        }

        var requested = read.Value!;
        var missingPart = FindMissingPart(requested);

        if (missingPart is not null)
        {
            return AiToolExecution.Failed(missingPart);
        }

        var wasEnabledAsked = arguments.TryGetProperty("isEnabled", out _);
        var executionUserId = requested.ExecutionUserId
            ?? await FindDefaultServiceAccount(requested.Actions, cancellationToken);

        if (executionUserId is null)
        {
            return AiToolExecution.Failed(
                "No enabled automation service account exists, so the rule has nothing to run as. "
                + "Ask the user to create one first.");
        }

        var request = requested with
        {
            IsEnabled = wasEnabledAsked && requested.IsEnabled,
            ExecutionUserId = executionUserId,
        };
        var rule = AiAutomationJson.Normalize(request, ActionRegistry);
        var error = await AiAutomationProposal.Validate(Mediator, rule, cancellationToken);

        if (error is not null)
        {
            return AiToolExecution.Failed(error);
        }

        var fields = await AiAutomationProposal.DescribeFields(Mediator, null, rule, cancellationToken);
        var payload = JsonSerializer.SerializeToDocument(new { rule }, AiAutomationJson.Options);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiAutomationProposal.EntityType,
            Summary = $"Create automation “{rule.Name}”",
            Fields = fields,
            Payload = payload,
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        var state = rule.IsEnabled ? "enabled" : "disabled";

        return AiToolExecution.Success(
            $"Proposed creating automation “{rule.Name}”, {state}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static string? FindMissingPart(AutomationRuleRequest rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            return "A rule name is required.";
        }

        if (rule.Trigger is null)
        {
            return "A trigger is required.";
        }

        if (rule.Actions is null)
        {
            return "An actions list is required.";
        }

        return null;
    }

    private async Task<string?> FindDefaultServiceAccount(
        List<AutomationActionRequest> actions,
        CancellationToken cancellationToken)
    {
        var accounts = await Mediator.Send(new GetServiceAccountsQuery(), cancellationToken);
        var requiredPermissions = AutomationPermissionPolicy.GetRequiredPermissions(actions, ActionRegistry);
        var enabledAccounts = accounts
            .Where(item => item.DisabledAt is null)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var capableAccount = enabledAccounts.FirstOrDefault(item =>
        {
            return AutomationPermissionPolicy.HasRequiredPermissions(item.Permissions, requiredPermissions);
        });
        var account = capableAccount ?? enabledAccounts.FirstOrDefault();

        return account?.UserId;
    }
}
