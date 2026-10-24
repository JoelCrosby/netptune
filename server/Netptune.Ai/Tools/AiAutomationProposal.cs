using System.Text.Json;
using System.Text.RegularExpressions;

using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.Automations.Queries;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.ServiceAccounts.Queries;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

internal static partial class AiAutomationProposal
{
    public const string EntityType = "automationRule";

    private const string Workspace = "Workspace";

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

        var actions = pendingCount == 1 ? "1 pending delayed action" : $"{pendingCount} pending delayed actions";

        return $"Applying this cancels {actions}, and undo will not bring them back.";
    }

    public static async Task<List<AiChangeField>> DescribeFields(
        IMediator mediator,
        AutomationRuleRequest? before,
        AutomationRuleRequest after,
        CancellationToken cancellationToken)
    {
        var fields = new List<AiChangeField>();
        var isCreate = before is null;

        AddTextWhenChanged(fields, "name", before?.Name, after.Name);
        AddTextWhenChanged(fields, "enabled", DescribeEnabled(before?.IsEnabled), DescribeEnabled(after.IsEnabled));

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

        var beforeTrigger = Humanize(before?.Trigger.Type.ToString());
        var afterTrigger = Humanize(after.Trigger.Type.ToString());

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

            return $"Project · {project?.Name ?? $"#{rule.ProjectId}"}";
        }

        if (rule.BoardId.HasValue)
        {
            var board = await AiBoardLookup.FindBoard(mediator, rule.BoardId.Value, cancellationToken);

            return $"Board · {board?.Name ?? $"#{rule.BoardId}"}";
        }

        if (rule.SprintId.HasValue)
        {
            var sprint = await AiSprintLookup.Find(mediator, rule.SprintId.Value, cancellationToken);

            return $"Sprint · {sprint?.Name ?? $"#{rule.SprintId}"}";
        }

        return Workspace;
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

    private static string? DescribeEnabled(bool? isEnabled)
    {
        return isEnabled switch
        {
            true => "Enabled",
            false => "Disabled",
            null => null,
        };
    }

    private static string? DescribeActions(AutomationRuleRequest? rule)
    {
        if (rule is null)
        {
            return null;
        }

        var names = rule.Actions.Select(action => Humanize(action.Type.ToString()));

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

    private static string? Humanize(string? name)
    {
        if (name is null)
        {
            return null;
        }

        var spaced = WordBoundary().Replace(name, " $1").ToLowerInvariant();

        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex WordBoundary();
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
