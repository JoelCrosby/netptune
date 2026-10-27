namespace Netptune.Core.Models.ProjectTasks;

public static class ProjectTaskEstimate
{
    public const decimal SmallestTShirtSize = 1;

    public const decimal LargestTShirtSize = 5;

    public static bool IsTShirtSize(decimal value)
    {
        return value is >= SmallestTShirtSize and <= LargestTShirtSize && value % 1 == 0;
    }
}
