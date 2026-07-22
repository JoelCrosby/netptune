using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Extensions;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class TaskChangedAutomationRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<TaskChangedAutomationRuleMatcher> Logger;

    public TaskChangedAutomationRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<TaskChangedAutomationRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<PendingAutomationExecution>> Match(TaskChangedMessage message, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        Logger.LogInformation(
            "Evaluating task-change automation rules for task {TaskId} in workspace {WorkspaceId} ({EventId})",
            message.TaskId,
            message.WorkspaceId,
            message.EventId);

        var taskChangedRules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            AutomationTriggerType.TaskChanged,
            message.WorkspaceId,
            cancellationToken);

        var statusRules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            AutomationTriggerType.TaskStatusChanged,
            message.WorkspaceId,
            cancellationToken);

        var rules = taskChangedRules.Concat(statusRules).ToList();

        Telemetry.RecordRulesEvaluated(AutomationTriggerType.TaskChanged, rules.Count);

        activity?.SetTag("automation.rules.evaluated", rules.Count);

        if (rules.Count == 0)
        {
            Logger.LogDebug(
                "No task-change automation rules found for workspace {WorkspaceId}",
                message.WorkspaceId);
            return [];
        }

        var task = await UnitOfWork.Tasks.GetAutomationTask(message.TaskId, cancellationToken);

        if (task is null)
        {
            Logger.LogWarning(
                "Task-change automation skipped for missing or deleted task {TaskId}",
                message.TaskId);
            activity?.SetTag("automation.skip_reason", "task_not_found");
            Telemetry.RecordRulesSkipped(AutomationTriggerType.TaskChanged, rules.Count, "task_not_found");

            return [];
        }

        var executions = new List<PendingAutomationExecution>();

        foreach (var rule in rules)
        {
            if (!Matches(rule, message))
            {
                continue;
            }

            executions.Add(new PendingAutomationExecution
            {
                Rule = rule,
                Task = task,
                ActorUserId = message.ActorUserId,
                IdempotencyKey = $"rule:{rule.Id}:task:{message.TaskId}:event:{message.EventId}",
                TriggeredAt = message.OccurredAt,
            });
        }

        Telemetry.RecordRulesMatched(AutomationTriggerType.TaskChanged, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} of {RuleCount} task-change automation rules for task {TaskId}",
            executions.Count,
            rules.Count,
            message.TaskId);

        return executions;
    }

    private bool Matches(AutomationRule rule, TaskChangedMessage message)
    {
        return rule.TriggerType switch
        {
            AutomationTriggerType.TaskChanged => MatchesTaskChangedRule(rule, message),
            AutomationTriggerType.TaskStatusChanged => MatchesStatusChangedRule(rule, message),
            _ => false,
        };
    }

    private bool MatchesTaskChangedRule(AutomationRule rule, TaskChangedMessage message)
    {
        var configuredFields = ConfigReader.ReadEnumList<TaskChangeField>(rule.TriggerConfig, "fields");
        var watchedFields = configuredFields.Count == 0
            ? Enum.GetValues<TaskChangeField>().ToHashSet()
            : configuredFields.ToHashSet();

        var conditions = ConfigReader.ReadList<AutomationFieldCondition>(rule.TriggerConfig, "conditions");

        foreach (var change in message.Changes)
        {
            if (!watchedFields.Contains(change.Field))
            {
                continue;
            }

            var condition = conditions.FirstOrDefault(candidate => candidate.Field == change.Field);

            if (MatchesFieldCondition(rule, change, condition))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesStatusChangedRule(AutomationRule rule, TaskChangedMessage message)
    {
        var statusChange = message.Changes.FirstOrDefault(change => change.Field == TaskChangeField.Status);

        return statusChange is not null && MatchesStatusCondition(rule, statusChange);
    }

    private static bool MatchesFieldCondition(AutomationRule rule, TaskFieldChange change, AutomationFieldCondition? condition)
    {
        if (condition is not null)
        {
            return MatchesCondition(condition, change);
        }

        var matches = change.Field switch
        {
            TaskChangeField.Status => MatchesStatusCondition(rule, change),
            TaskChangeField.Assignees => MatchesAssigneeCondition(rule, change),
            _ => true,
        };

        return matches;
    }

    private static bool MatchesCondition(AutomationFieldCondition condition, TaskFieldChange change)
    {
        return condition.Operator switch
        {
            AutomationConditionOperator.Any => true,
            AutomationConditionOperator.Equals => change.NewValue.EqualsOrdinalIgnoreCase(condition.Value),
            AutomationConditionOperator.NotEquals => !change.NewValue.EqualsOrdinalIgnoreCase(condition.Value),
            AutomationConditionOperator.Contains => change.NewValue.ContainsOrdinalIgnoreCase(condition.Value),
            AutomationConditionOperator.IsEmpty => string.IsNullOrWhiteSpace(change.NewValue),
            AutomationConditionOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(change.NewValue),
            AutomationConditionOperator.Added => MatchesCollection(change.AddedValues, condition.Value),
            AutomationConditionOperator.Removed => MatchesCollection(change.RemovedValues, condition.Value),
            _ => false,
        };
    }

    private static bool MatchesCollection(IReadOnlyCollection<string> values, string? expected)
    {
        return string.IsNullOrWhiteSpace(expected)
            ? values.Count > 0
            : values.Any(value => value.EqualsOrdinalIgnoreCase(expected));
    }

    private static bool MatchesStatusCondition(AutomationRule rule, TaskFieldChange change)
    {
        var configuredStatusId = ConfigReader.ReadInt(rule.TriggerConfig, "statusId");

        if (configuredStatusId.HasValue)
        {
            return int.TryParse(change.NewValue, out var newStatusId) &&
                   newStatusId == configuredStatusId.Value;
        }

        return true;
    }

    private static bool MatchesAssigneeCondition(AutomationRule rule, TaskFieldChange change)
    {
        var mode = ConfigReader.ReadEnum<AssigneeChangeMode>(rule.TriggerConfig, "assigneeChangeMode") ??
                   AssigneeChangeMode.AddedOrRemoved;

        return mode switch
        {
            AssigneeChangeMode.Added => change.AddedValues.Count > 0,
            AssigneeChangeMode.Removed => change.RemovedValues.Count > 0,
            _ => change.AddedValues.Count > 0 || change.RemovedValues.Count > 0,
        };
    }
}
