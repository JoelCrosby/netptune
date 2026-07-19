namespace Netptune.Core.Models.ProjectTasks;

public static class ProjectTaskSchedule
{
    public const string InvalidDateRangeMessage = "Task start date must be on or before its due date";

    public static bool IsValid(DateOnly? startDate, DateOnly? dueDate)
    {
        var hasCompleteRange = startDate.HasValue && dueDate.HasValue;

        if (!hasCompleteRange)
        {
            return true;
        }

        return startDate.GetValueOrDefault() <= dueDate.GetValueOrDefault();
    }
}
