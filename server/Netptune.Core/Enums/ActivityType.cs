namespace Netptune.Core.Enums;

public enum ActivityType
{
    Create = 0,
    Modify = 1,
    Delete = 2,
    Assign = 3,
    Move = 5,
    Reorder = 6,
    Flag = 7,
    UnFlag = 8,
    ModifyName = 9,
    ModifyDescription = 10,
    ModifyStatus = 11,
}

public enum ActivitySubType
{
    MoveTaskToGroup = 0,
    FlagTask,
}