namespace Netptune.Core.Enums
{
    public enum BoardGroupType
    {
        Basic = 0,

        Backlog = 1,

        Done = 2,

        Todo = 3,
    }

    public static class BoardGroupTypeExtensions
    {
        public static ProjectTaskStatus GetTaskStatusFromGroupType(this BoardGroupType type)
        {
            return type switch
            {
                BoardGroupType.Todo => ProjectTaskStatus.InProgress,
                BoardGroupType.Done => ProjectTaskStatus.Complete,
                BoardGroupType.Backlog => ProjectTaskStatus.InActive,
                _ => ProjectTaskStatus.InActive,
            };
        }
    }
}
