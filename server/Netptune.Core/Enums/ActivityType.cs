namespace Netptune.Core.Enums;

public enum ActivityType
{
    Create = 0,
    Modify = 1,
    Delete = 2,
    Assign = 3,

    // 4 is an intentional gap — reserved, do not reuse.

    Move = 5,
    Reorder = 6,

    // 7 and 8 are intentional gaps — reserved, do not reuse.

    ModifyName = 9,
    ModifyDescription = 10,
    ModifyStatus = 11,
    Invite = 12,
    Remove = 13,
    PermissionChanged = 14,
    Unassign = 15,
    AddTag = 16,
    RemoveTag = 17,
    RoleChanged = 18,
    WorkspaceSettingsChanged = 19,
    ExportRequested = 20,
    LoginSuccess = 21,
    LoginFailed = 22,
    Mention = 23,
    ModifyPriority = 24,
    ModifyEstimate = 25,
    Restore = 26,
    AddRelation = 27,
    RemoveRelation = 28,
    ModifyDueDate = 29,
    AddFile = 30,
    RemoveFile = 31,
    ModifyStartDate = 32,
}
