namespace Netptune.Core.Enums;

public enum AutomationTriggerType
{
    TaskUnassignedFor = 1,
    TaskChanged = 2,
    TaskDueDateApproaching = 3,
    TaskCreated = 4,
    TaskOverdue = 5,
    TaskHasNoDueDate = 6,
    TaskInactiveFor = 7,
    SprintStarted = 8,
    SprintCompleted = 9,
    SprintEndingSoon = 10,
    TaskBlocked = 11,
    TaskUnblocked = 12,
    SubtasksCompleted = 13,
}
