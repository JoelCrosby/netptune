using System.Text.Json;

using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.Automations.Queries;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.ServiceAccounts.Queries;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

internal static class AiAutomationProposal
{
    public const string EntityType = "automationRule";

    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web);

    public static AiAutomationRead<T> Read<T>(JsonElement element, string what) where T : class
    {
        try
        {
            var value = element.Deserialize<T>(AiAutomationJson.Options);

            return value is null
                ? AiAutomationRead<T>.Failed($"The {what} is missing.")
                : AiAutomationRead<T>.Read(value);
        }
        catch (JsonException exception)
        {
            return AiAutomationRead<T>.Failed($"The {what} could not be read: {exception.Message}");
        }
    }

    public static async Task<string?> Validate(
        IMediator mediator,
        AutomationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await mediator.Send(new ValidateAutomationRuleQuery(request), cancellationToken);

        return validation.IsSuccess ? null : validation.Message ?? "The rule is not valid.";
    }

    public static async Task<string?> CancellationNotice(
        IMediator mediator,
        int ruleId,
        CancellationToken cancellationToken)
    {
        var pendingCount = await mediator.Send(new CountPendingRuleActionsQuery(ruleId), cancellationToken);

        if (pendingCount == 0)
        {
            return null;
        }

        return pendingCount == 1
            ? AppText.AiAutomationCancelsOnePendingAction
            : Localized.Format(AppText.AiAutomationCancelsPendingActions, pendingCount);
    }

    public static async Task<List<AiChangeField>> DescribeFields(
        IMediator mediator,
        AutomationRuleRequest? before,
        AutomationRuleRequest after,
        CancellationToken cancellationToken)
    {
        var fields = new List<AiChangeField>();
        var isCreate = before is null;
        var beforeState = DescribeEnabledIfSet(before?.IsEnabled);
        var afterState = DescribeEnabled(after.IsEnabled);

        AddTextWhenChanged(fields, "name", before?.Name, after.Name);
        AddTextWhenChanged(fields, "enabled", beforeState, afterState);

        var isRescoped = isCreate
            || before!.ProjectId != after.ProjectId
            || before.BoardId != after.BoardId
            || before.SprintId != after.SprintId;

        if (isRescoped)
        {
            var beforeScope = before is null ? null : await DescribeScope(mediator, before, cancellationToken);
            var afterScope = await DescribeScope(mediator, after, cancellationToken);

            fields.Add(AiChangeFields.Text("scope", beforeScope, afterScope));
        }

        var isRunsAsChanged = before?.ExecutionUserId != after.ExecutionUserId;

        if (isRunsAsChanged)
        {
            var accounts = await mediator.Send(new GetServiceAccountsQuery(), cancellationToken);
            var beforeAccount = DescribeAccount(accounts, before?.ExecutionUserId);
            var afterAccount = DescribeAccount(accounts, after.ExecutionUserId);

            fields.Add(AiChangeFields.Values("runsAs", AiChangeValueKind.User, beforeAccount, afterAccount));
        }

        var beforeTrigger = before is null ? null : LocalizedEnum.Label(before.Trigger.Type);
        var afterTrigger = LocalizedEnum.Label(after.Trigger.Type);

        AddTextWhenChanged(fields, "trigger", beforeTrigger, afterTrigger);
        AddTextWhenChanged(fields, "actions", DescribeActions(before), DescribeActions(after));

        var snapshot = await SnapshotField(mediator, before, after, cancellationToken);

        fields.Add(snapshot);

        return fields;
    }

    private static async Task<AiChangeField> SnapshotField(
        IMediator mediator,
        AutomationRuleRequest? before,
        AutomationRuleRequest after,
        CancellationToken cancellationToken)
    {
        var statuses = await mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken) ?? [];
        var beforeSnapshot = before is null ? null : ToSnapshot(before, statuses);
        var afterSnapshot = ToSnapshot(after, statuses);

        return new AiChangeField
        {
            Name = "rule",
            Kind = AiChangeValueKind.Automation,
            Before = beforeSnapshot,
            After = afterSnapshot,
        };
    }

    private static string ToSnapshot(AutomationRuleRequest rule, List<StatusViewModel> statuses)
    {
        var snapshot = new
        {
            trigger = rule.Trigger,
            actions = rule.Actions,
            statuses = statuses.Select(status => new
            {
                id = status.Id,
                name = status.Name,
                color = status.Color,
                category = status.Category,
            }),
        };

        return JsonSerializer.Serialize(snapshot, SnapshotOptions);
    }

    private static async Task<string> DescribeScope(
        IMediator mediator,
        AutomationRuleRequest rule,
        CancellationToken cancellationToken)
    {
        if (rule.ProjectId.HasValue)
        {
            var projects = await mediator.Send(new GetProjectsQuery(), cancellationToken);
            var project = projects.FirstOrDefault(item => item.Id == rule.ProjectId.Value);

            return Localized.Format(AppText.AiAutomationScopeProject, project?.Name ?? $"#{rule.ProjectId}");
        }

        if (rule.BoardId.HasValue)
        {
            var board = await AiBoardLookup.FindBoard(mediator, rule.BoardId.Value, cancellationToken);

            return Localized.Format(AppText.AiAutomationScopeBoard, board?.Name ?? $"#{rule.BoardId}");
        }

        if (rule.SprintId.HasValue)
        {
            var sprint = await AiSprintLookup.Find(mediator, rule.SprintId.Value, cancellationToken);

            return Localized.Format(AppText.AiAutomationScopeSprint, sprint?.Name ?? $"#{rule.SprintId}");
        }

        return AppText.AiAutomationScopeWorkspace;
    }

    private static List<AiChangeValue> DescribeAccount(
        List<ServiceAccountViewModel> accounts,
        string? userId)
    {
        if (userId is null)
        {
            return [];
        }

        var account = accounts.FirstOrDefault(item => item.UserId == userId);

        return [AiChangeFields.User(userId, account?.Name ?? userId)];
    }

    public static string DescribeEnabled(bool isEnabled)
    {
        return isEnabled ? AppText.AiAutomationEnabled : AppText.AiAutomationDisabled;
    }

    private static string? DescribeEnabledIfSet(bool? isEnabled)
    {
        return isEnabled.HasValue ? DescribeEnabled(isEnabled.Value) : null;
    }

    private static string? DescribeActions(AutomationRuleRequest? rule)
    {
        if (rule is null)
        {
            return null;
        }

        var names = rule.Actions.Select(action => LocalizedEnum.Label(action.Type));

        return string.Join(", ", names);
    }

    private static void AddTextWhenChanged(List<AiChangeField> fields, string name, string? before, string? after)
    {
        var isChanged = !string.Equals(before, after, StringComparison.Ordinal);

        if (isChanged)
        {
            fields.Add(AiChangeFields.Text(name, before, after));
        }
    }
}

internal sealed record AiAutomationRead<T> where T : class
{
    public T? Value { get; private init; }

    public string? Error { get; private init; }

    public static AiAutomationRead<T> Read(T value)
    {
        return new AiAutomationRead<T> { Value = value };
    }

    public static AiAutomationRead<T> Failed(string error)
    {
        return new AiAutomationRead<T> { Error = error };
    }
}
