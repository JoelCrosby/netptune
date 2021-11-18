namespace Netptune.Core.Enums;

public enum ProjectTaskStatus
{
    New = 0,

    Complete = 1,

    InProgress = 2,

    OnHold = 3,

    UnAssigned = 4,

    AwaitingClassification = 5,

    InActive = 6,
}

public static class ProjectTaskStatusExtensions
{
    public static BoardGroupType GetGroupTypeFromTaskStatus(this ProjectTaskStatus status)
    {
        return status switch
        {
            ProjectTaskStatus.New => BoardGroupType.Todo,
            ProjectTaskStatus.InActive => BoardGroupType.Todo,
            ProjectTaskStatus.Complete => BoardGroupType.Done,
            ProjectTaskStatus.AwaitingClassification => BoardGroupType.Todo,
            ProjectTaskStatus.UnAssigned => BoardGroupType.Backlog,
            _ => BoardGroupType.Backlog,
        };
    }
}