using System.Text.Json.Nodes;

using Netptune.Automation.Rules;
using Netptune.Core.Constants;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Ai.Tools;

public static class AiAutomationReference
{
    public static IReadOnlyDictionary<AutomationTriggerType, string> Triggers { get; } =
        new Dictionary<AutomationTriggerType, string>
        {
            [AutomationTriggerType.TaskCreated] = "A task is created.",
            [AutomationTriggerType.TaskChanged] =
                "One of the watched task fields changes. Needs fields (TaskChangeField names); its conditions may use "
                + "the change operators Any, Added and Removed.",
            [AutomationTriggerType.TaskUnassignedFor] = "A task has had no assignee for durationDays (1-365).",
            [AutomationTriggerType.TaskDueDateApproaching] = "A task is due within durationDays (0-365).",
            [AutomationTriggerType.TaskOverdue] = "A task is past its due date.",
            [AutomationTriggerType.TaskHasNoDueDate] = "A task has no due date.",
            [AutomationTriggerType.TaskInactiveFor] = "A task has not changed for durationDays (1-365).",
            [AutomationTriggerType.TaskBlocked] = "A task goes from no incomplete blocking dependency to one.",
            [AutomationTriggerType.TaskUnblocked] = "A task's last incomplete blocking dependency is done or unlinked.",
            [AutomationTriggerType.SubtasksCompleted] = "Every subtask of a task is done; actions run on the parent.",
            [AutomationTriggerType.SprintStarted] = "A sprint starts; actions run on its tasks.",
            [AutomationTriggerType.SprintCompleted] = "A sprint completes; actions run on its tasks.",
            [AutomationTriggerType.SprintEndingSoon] = "A sprint ends within durationDays (0-365).",
            [AutomationTriggerType.PullRequestOpened] =
                "A pull request linked to a task is opened. Optional targetBranches (names, a trailing * matches a "
                + $"prefix, at most {AutomationSourceControlTrigger.MaxTargetBranches}) and includeDrafts.",
            [AutomationTriggerType.PullRequestMerged] =
                "A pull request linked to a task is merged. Optional targetBranches as for PullRequestOpened.",
            [AutomationTriggerType.ChecksFailed] =
                "Checks fail on a commit linked to a task through a pull request, branch or commit.",
        };

    public static IReadOnlyDictionary<TaskChangeField, string> ConditionFields { get; } =
        new Dictionary<TaskChangeField, string>
        {
            [TaskChangeField.Name] = "Text.",
            [TaskChangeField.Description] = "Text.",
            [TaskChangeField.Status] = "A status id from list_records.",
            [TaskChangeField.Assignees] = "A member id; a collection, so it also takes Added and Removed.",
            [TaskChangeField.Owner] = "A member id.",
            [TaskChangeField.Priority] = "A TaskPriority name: None, Low, Medium, High or Critical.",
            [TaskChangeField.Estimate] = "An EstimateType name.",
            [TaskChangeField.DueDate] = "A yyyy-MM-dd date.",
            [TaskChangeField.Tags] = "A tag name; a collection, so it also takes Added and Removed.",
            [TaskChangeField.StartDate] = "A yyyy-MM-dd date.",
            [TaskChangeField.Sprint] = "A sprint id from list_records.",
            [TaskChangeField.BoardGroup] = "TaskChanged with Any only: the task moved to another board group.",
        };

    public static IReadOnlyDictionary<AutomationConditionOperator, string> ConditionOperators { get; } =
        new Dictionary<AutomationConditionOperator, string>
        {
            [AutomationConditionOperator.Equals] = "The field has value.",
            [AutomationConditionOperator.NotEquals] = "The field does not have value.",
            [AutomationConditionOperator.Contains] = "Text fields contain value, or a collection field includes it.",
            [AutomationConditionOperator.IsEmpty] = "The field has no value; value is ignored.",
            [AutomationConditionOperator.IsNotEmpty] = "The field has a value; value is ignored.",
            [AutomationConditionOperator.Any] = "TaskChanged only: the field changed at all; value is ignored.",
            [AutomationConditionOperator.Added] = "TaskChanged only, collection fields: value was added.",
            [AutomationConditionOperator.Removed] = "TaskChanged only, collection fields: value was removed.",
        };

    public static IReadOnlyDictionary<AutomationActionType, string> Actions { get; } =
        new Dictionary<AutomationActionType, string>
        {
            [AutomationActionType.NotifyTaskAssignees] =
                "Sends a notification. recipients (AutomationNotificationRecipient names); recipientUserIds when "
                + "SpecificUsers is a recipient; recipientRoles (WorkspaceRole names) when WorkspaceRoles is; "
                + "optional message, which may use the variables below.",
            [AutomationActionType.FlagTask] = "Raises a flag on the task. flagName, optional flagDescription.",
            [AutomationActionType.UpdateTask] =
                "Changes the task. At least one of statusId, priority, taskName, taskDescription or clearDescription, "
                + "ownerId or clearOwner, assigneeIds (replaces the assignees, or adds to them with appendAssignees), addTags, removeTags, startDate, "
                + "dueDate, estimateType with estimateValue or clearEstimate, sprintId or clearSprint, boardGroupId.",
            [AutomationActionType.AddComment] = "Comments on the task. comment, which may use the variables below.",
            [AutomationActionType.DeleteTask] =
                "Deletes the task. Optional delayAmount with delayUnit (Minutes, Hours or Days; at most 365 days) "
                + "waits first, and is cancelled if the task's status changes meanwhile.",
            [AutomationActionType.CreateTask] =
                "Creates a task. taskName (may use the variables below), optional taskDescription, statusId, "
                + "priority, assigneeIds or copyAssignees, addTags, startDate, dueDate, sprintId, boardGroupId, "
                + "and linkRelationTypeId to link the new task back to the triggering one.",
            [AutomationActionType.ManageTaskRelation] =
                "Links or unlinks the task. relationOperation (Add or Remove), relationDirection (TaskIsSource or "
                + "TaskIsTarget), relationTypeId, relatedTaskId.",
        };

    public static JsonObject Build()
    {
        var variables = AutomationMessageTemplate.Variables.Select(variable => $"{{{{{variable}}}}}");

        return new JsonObject
        {
            ["note"] =
                "Enums are written by name. A rule needs a name, executionUserId (list_records "
                + "automation_service_accounts), a trigger and 1 to "
                + $"{AutomationValidation.MaximumActionCount} actions. At most one of projectId, boardId or sprintId "
                + "scopes the rule; none means the whole workspace.",
            ["triggers"] = ToObject(Triggers),
            ["conditionGroup"] =
                "Optional on trigger. {operator: All|Any|None, conditions: [{field, operator, value}], groups: [nested "
                + $"groups]}}; at most {ConditionGroupLimits.MaximumDepth} levels and "
                + $"{ConditionGroupLimits.MaximumConditionCount} conditions. value is always a string.",
            ["conditionFields"] = ToObject(ConditionFields),
            ["conditionOperators"] = ToObject(ConditionOperators),
            ["actions"] = ToObject(Actions),
            ["dates"] =
                "startDate and dueDate are {mode, date, offset}: Absolute uses date (yyyy-MM-dd), RelativeDays and "
                + "RelativeBusinessDays use offset from today, Clear removes the date.",
            ["messageVariables"] = string.Join(", ", variables),
        };
    }

    private static JsonObject ToObject<TKey>(IReadOnlyDictionary<TKey, string> descriptions) where TKey : struct, Enum
    {
        var entries = descriptions.Select(entry =>
        {
            return KeyValuePair.Create<string, JsonNode?>(entry.Key.ToString(), entry.Value);
        });

        return new JsonObject(entries);
    }
}
