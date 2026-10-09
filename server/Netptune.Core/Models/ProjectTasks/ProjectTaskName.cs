namespace Netptune.Core.Models.ProjectTasks;

public static class ProjectTaskName
{
    public const int MaxLength = 1024;

    public const string TooLongMessage = "Task name cannot exceed 1024 characters";

    public static bool IsValid(string? name)
    {
        return name is null || name.Length <= MaxLength;
    }
}
