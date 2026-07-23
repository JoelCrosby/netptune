using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
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
        var correlationId = message.CorrelationId ?? message.EventId;
        var selfTriggerSkippedCount = 0;

        foreach (var rule in rules)
        {
            var isAutomationEvent = message.OriginType == EventOriginType.Automation;
            var isSourceRule = message.AutomationRuleId == rule.Id;
            var isSelfTrigger = isAutomationEvent && isSourceRule;

            if (isSelfTrigger)
            {
                selfTriggerSkippedCount++;

                Logger.LogWarning(
                    "Automation rule {RuleId} skipped its own task-change event {EventId} at chain depth {ChainDepth}",
                    rule.Id,
                    message.EventId,
                    message.ChainDepth);

                continue;
            }

            if (!Matches(rule, message, task))
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
                CorrelationId = correlationId,
                CausationEventId = message.EventId,
                ChainDepth = message.ChainDepth,
            });
        }

        if (selfTriggerSkippedCount > 0)
        {
            Telemetry.RecordRulesSkipped(
                AutomationTriggerType.TaskChanged,
                selfTriggerSkippedCount,
                "self_trigger");
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

    private bool Matches(AutomationRule rule, TaskChangedMessage message, ProjectTask task)
    {
        return rule.TriggerType switch
        {
            AutomationTriggerType.TaskChanged => MatchesTaskChangedRule(rule, message, task),
            AutomationTriggerType.TaskStatusChanged => MatchesStatusChangedRule(rule, message),
            _ => false,
        };
    }

    private bool MatchesTaskChangedRule(AutomationRule rule, TaskChangedMessage message, ProjectTask task)
    {
        var configuredFields = JsonUtils.ReadEnumList<TaskChangeField>(rule.TriggerConfig, "fields");
        var watchesAllFields = configuredFields.Count == 0;
        var allTaskFields = Enum.GetValues<TaskChangeField>().ToHashSet();
        var configuredFieldSet = configuredFields.ToHashSet();
        var watchedFields = watchesAllFields
            ? allTaskFields
            : configuredFieldSet;

        var matchingChanges = message.Changes
            .Where(change => watchedFields.Contains(change.Field))
            .ToList();

        if (matchingChanges.Count == 0)
        {
            return false;
        }

        var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(rule.TriggerConfig, "conditionGroup");

        if (conditionGroup is not null)
        {
            return conditionGroup.Matches(task, message);
        }

        var conditions = JsonUtils.ReadList<AutomationFieldCondition>(rule.TriggerConfig, "conditions");

        foreach (var change in matchingChanges)
        {
            var condition = conditions.FirstOrDefault(candidate => candidate.Field == change.Field);

            if (MatchesLegacyFieldCondition(rule, change, condition))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesStatusChangedRule(AutomationRule rule, TaskChangedMessage message)
    {
        var statusChange = message.Changes.FirstOrDefault(change => change.Field == TaskChangeField.Status);

        if (statusChange is null)
        {
            return false;
        }

        return MatchesStatusCondition(rule, statusChange);
    }

    private static bool MatchesLegacyFieldCondition(
        AutomationRule rule,
        TaskFieldChange change,
        AutomationFieldCondition? condition)
    {
        if (condition is not null)
        {
            return condition.Matches(change);
        }

        var matches = change.Field switch
        {
            TaskChangeField.Status => MatchesStatusCondition(rule, change),
            TaskChangeField.Assignees => MatchesAssigneeCondition(rule, change),
            _ => true,
        };

        return matches;
    }

    private static bool MatchesStatusCondition(AutomationRule rule, TaskFieldChange change)
    {
        var configuredStatusId = JsonUtils.ReadInt(rule.TriggerConfig, "statusId");

        if (configuredStatusId.HasValue)
        {
            var hasNewStatusId = int.TryParse(change.NewValue, out var newStatusId);
            var matchesConfiguredStatus = hasNewStatusId && newStatusId == configuredStatusId.Value;

            return matchesConfiguredStatus;
        }

        return true;
    }

    private static bool MatchesAssigneeCondition(AutomationRule rule, TaskFieldChange change)
    {
        var mode = JsonUtils.ReadEnum<AssigneeChangeMode>(rule.TriggerConfig, "assigneeChangeMode") ?? AssigneeChangeMode.AddedOrRemoved;
        var hasAddedAssignees = change.AddedValues.Count > 0;
        var hasRemovedAssignees = change.RemovedValues.Count > 0;
        var hasChangedAssignees = hasAddedAssignees || hasRemovedAssignees;

        return mode switch
        {
            AssigneeChangeMode.Added => hasAddedAssignees,
            AssigneeChangeMode.Removed => hasRemovedAssignees,
            _ => hasChangedAssignees,
        };
    }
}
