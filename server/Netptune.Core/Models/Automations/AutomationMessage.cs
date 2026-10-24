using System.Globalization;

using Netptune.Core.Models.ProjectTasks;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationMessage
{
    public string? Code { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public required string Text { get; init; }

    public static AutomationMessage Uncoded(string text)
    {
        return new AutomationMessage { Text = text };
    }

    public static AutomationMessage ActionNotExecutable { get; } =
        Coded("actionNotExecutable", "The action did not produce an executable contribution.");

    public static AutomationMessage TaskDeletedByEarlierAction { get; } =
        Coded("taskDeletedByEarlierAction", "The task was deleted by an earlier action.");

    public static AutomationMessage NoExecutionHandler { get; } =
        Coded("noExecutionHandler", "No execution handler is registered for the action type.");

    public static AutomationMessage ActionTypeUnsupported { get; } =
        Coded("actionTypeUnsupported", "The action type is not supported.");

    public static AutomationMessage CancelledAfterPlanningFailure { get; } =
        Coded("cancelledAfterPlanningFailure", "The run was cancelled because a later action could not be planned.");

    public static AutomationMessage NotPlannedAfterEarlierFailure { get; } =
        Coded("notPlannedAfterEarlierFailure", "The action was not planned because an earlier action failed.");

    public static AutomationMessage NotRunWhileUnauthorized { get; } =
        Coded("notRunWhileUnauthorized", "The action was not run because the automation principal was not authorized.");

    public static AutomationMessage PrincipalMissing { get; } =
        Coded("principalMissing", "The automation rule has no execution principal.");

    public static AutomationMessage PrincipalUnavailable { get; } =
        Coded("principalUnavailable", "The automation execution principal is no longer available.");

    public static AutomationMessage PrincipalDisabled { get; } =
        Coded("principalDisabled", "The automation execution principal is disabled.");

    public static AutomationMessage PrincipalLacksPermissions { get; } =
        Coded(
            "principalLacksPermissions",
            "The automation execution principal no longer has the required permissions.");

    public static AutomationMessage DeleteScheduled { get; } =
        Coded("deleteScheduled", "The action was scheduled for later execution.");

    public static AutomationMessage TaskAlreadyDeleted { get; } =
        Coded("taskAlreadyDeleted", "The task was already deleted.");

    public static AutomationMessage TaskAlreadyFlagged { get; } =
        Coded("taskAlreadyFlagged", "The task already has a flag from this rule.");

    public static AutomationMessage TaskAlreadyMatches { get; } =
        Coded("taskAlreadyMatches", "The task already has the configured values.");

    public static AutomationMessage TaskNotInProject { get; } =
        Coded("taskNotInProject", "The triggering task is not in a project.");

    public static AutomationMessage TaskKeyUnavailable { get; } =
        Coded("taskKeyUnavailable", "Unable to reserve a task key for the project.");

    public static AutomationMessage ProjectUnavailable { get; } =
        Coded("projectUnavailable", "The triggering task's project is no longer available.");

    public static AutomationMessage BoardGroupUnavailable { get; } =
        Coded("boardGroupUnavailable", "The selected board group is not available for the project.");

    public static AutomationMessage BoardGroupUnavailableForTask { get; } =
        Coded("boardGroupUnavailableForTask", "The selected board group is not available for the task's project.");

    public static AutomationMessage TaskStatusUnavailable { get; } =
        Coded("taskStatusUnavailable", "A task status is not available in the workspace.");

    public static AutomationMessage SprintUnavailableForTask { get; } =
        Coded("sprintUnavailableForTask", "The selected sprint is not available for the task's project.");

    public static AutomationMessage StatusUnavailable { get; } =
        Coded("statusUnavailable", "The selected status is not available in the workspace.");

    public static AutomationMessage AssigneeUnavailable { get; } =
        Coded("assigneeUnavailable", "An assignee is no longer available in the workspace.");

    public static AutomationMessage OwnerOrAssigneeUnavailable { get; } =
        Coded("ownerOrAssigneeUnavailable", "An owner or assignee is no longer available in the workspace.");

    public static AutomationMessage TagUnavailable { get; } =
        Coded("tagUnavailable", "A configured tag is no longer available in the workspace.");

    public static AutomationMessage SprintUnavailable { get; } =
        Coded("sprintUnavailable", "The selected sprint is not available for the project.");

    public static AutomationMessage InvalidDateRange { get; } =
        Coded("invalidDateRange", ProjectTaskSchedule.InvalidDateRangeMessage);

    public static AutomationMessage TaskMissing { get; } =
        Coded("taskMissing", "The task no longer exists.");

    public static AutomationMessage TaskReloadFailed { get; } =
        Coded("taskReloadFailed", "The updated task could not be reloaded.");

    public static AutomationMessage RelationTypeUnavailable { get; } =
        Coded("relationTypeUnavailable", "The selected relation type is no longer available in the workspace.");

    public static AutomationMessage RelationTargetMissing { get; } =
        Coded("relationTargetMissing", "The action has no task to link to.");

    public static AutomationMessage RelationToSelf { get; } =
        Coded("relationToSelf", "A task cannot be related to itself.");

    public static AutomationMessage RelationTargetUnavailable { get; } =
        Coded("relationTargetUnavailable", "The task to link to is no longer available in the workspace.");

    public static AutomationMessage RelationAlreadyLinked { get; } =
        Coded("relationAlreadyLinked", "These tasks are already linked by this relation.");

    public static AutomationMessage RelationNothingToRemove { get; } =
        Coded("relationNothingToRemove", "The task has no relations of this type to remove.");

    public static AutomationMessage CreatedTaskAlreadyRelated { get; } =
        Coded("createdTaskAlreadyRelated", "The created task already has a relation of the selected type.");

    public static AutomationMessage RelationSingleSource(string relationTypeName)
    {
        return Coded(
            "relationSingleSource",
            $"That task already has a \"{relationTypeName}\" link. A task can only have one.",
            relationTypeName);
    }

    public static AutomationMessage RelationCycle(string relationTypeName)
    {
        return Coded("relationCycle", $"This would create a circular \"{relationTypeName}\" chain.", relationTypeName);
    }

    public static AutomationMessage DisabledAfterFailures(int failureCount, TimeSpan window)
    {
        return Coded(
            "disabledAfterFailures",
            $"Disabled automatically after {failureCount} failed runs within {DescribeWindow(window)}.",
            Format(failureCount),
            Format(window));
    }

    public static AutomationMessage DisabledAfterRuns(int runCount, TimeSpan window)
    {
        return Coded(
            "disabledAfterRuns",
            $"Disabled automatically after {runCount} runs within {DescribeWindow(window)}.",
            Format(runCount),
            Format(window));
    }

    private static AutomationMessage Coded(string code, string text, params string[] arguments)
    {
        return new AutomationMessage { Code = code, Text = text, Arguments = [.. arguments] };
    }

    private static string Format(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Format(TimeSpan window)
    {
        var minutes = Math.Round(window.TotalMinutes);

        return minutes.ToString(CultureInfo.InvariantCulture);
    }

    private static string DescribeWindow(TimeSpan window)
    {
        if (window.TotalHours < 1)
        {
            return $"{Math.Round(window.TotalMinutes)} minutes";
        }

        var tenthsOfAnHour = (int)Math.Round(window.TotalHours * 10);

        if (tenthsOfAnHour == 10)
        {
            return "an hour";
        }

        return $"{tenthsOfAnHour / 10d} hours";
    }
}
