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
}
