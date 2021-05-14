namespace Netptune.Core.Enums
{
    public enum ActivityType
    {
        Create = 0,
        Modify = 1,
        Delete = 2,
        Assign = 3,
        Move = 5,
        Reorder = 6,
    }

    public enum ActivitySubType
    {
        MoveTaskToGroup = 0,
        FlagTask,
    }
}
