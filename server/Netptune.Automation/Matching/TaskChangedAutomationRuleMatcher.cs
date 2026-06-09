using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class TaskChangedAutomationRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<TaskChangedAutomationRuleMatcher> Logger;

    public TaskChangedAutomationRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ILogger<TaskChangedAutomationRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<PendingAutomationExecution>> Match(
        TaskChangedMessage message,
        CancellationToken cancellationToken)
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
        var legacyStatusRules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            AutomationTriggerType.TaskStatusChanged,
            message.WorkspaceId,
            cancellationToken);
        var rules = taskChangedRules.Concat(legacyStatusRules).ToList();

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

        var executions = rules
            .Where(rule => Matches(rule, message))
            .Select(rule => new PendingAutomationExecution
            {
                Rule = rule,
                Task = task,
                ActorUserId = message.ActorUserId,
                IdempotencyKey = $"rule:{rule.Id}:task:{message.TaskId}:event:{message.EventId}",
            })
            .ToList();

        Telemetry.RecordRulesMatched(AutomationTriggerType.TaskChanged, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} of {RuleCount} task-change automation rules for task {TaskId}",
            executions.Count,
            rules.Count,
            message.TaskId);

        return executions;
    }

    private static bool Matches(AutomationRule rule, TaskChangedMessage message)
    {
        return rule.TriggerType switch
        {
            AutomationTriggerType.TaskChanged => MatchesTaskChangedRule(rule, message),
            AutomationTriggerType.TaskStatusChanged => MatchesLegacyStatusChangedRule(rule, message),
            _ => false,
        };
    }

    private static bool MatchesTaskChangedRule(AutomationRule rule, TaskChangedMessage message)
    {
        var configuredFields = ConfigReader.ReadEnumList<TaskChangeField>(rule.TriggerConfig, "fields");
        var watchedFields = configuredFields.Count == 0
            ? Enum.GetValues<TaskChangeField>().ToHashSet()
            : configuredFields.ToHashSet();

        return message.Changes.Any(change =>
            watchedFields.Contains(change.Field) &&
            MatchesFieldCondition(rule, change));
    }

    private static bool MatchesLegacyStatusChangedRule(AutomationRule rule, TaskChangedMessage message)
    {
        var statusChange = message.Changes.FirstOrDefault(change => change.Field == TaskChangeField.Status);

        return statusChange is not null && MatchesStatusCondition(rule, statusChange);
    }

    private static bool MatchesFieldCondition(AutomationRule rule, TaskFieldChange change)
    {
        return change.Field switch
        {
            TaskChangeField.Status => MatchesStatusCondition(rule, change),
            TaskChangeField.Assignees => MatchesAssigneeCondition(rule, change),
            _ => true,
        };
    }

    private static bool MatchesStatusCondition(AutomationRule rule, TaskFieldChange change)
    {
        var configuredStatus = ConfigReader.ReadEnum<ProjectTaskStatus>(rule.TriggerConfig, "status");

        if (configuredStatus is null)
        {
            return true;
        }

        return Enum.TryParse<ProjectTaskStatus>(change.NewValue, out var newStatus) &&
               newStatus == configuredStatus.Value;
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
