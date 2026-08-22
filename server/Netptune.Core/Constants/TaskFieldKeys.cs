namespace Netptune.Core.Constants;

// Declaring a key here does not oblige any catalog to offer it; each exposes the subset it supports.
public static class TaskFieldKeys
{
    public const string SystemId = "task.system_id";
    public const string Name = "task.name";
    public const string Description = "task.description";
    public const string Status = "task.status";
    public const string StatusCategory = "task.status_category";
    public const string Priority = "task.priority";
    public const string EstimateType = "task.estimate_type";
    public const string EstimateValue = "task.estimate_value";
    public const string StartDate = "task.start_date";
    public const string DueDate = "task.due_date";
    public const string Project = "task.project";
    public const string Sprint = "task.sprint";
    public const string BoardGroup = "task.board_group";
    public const string Owner = "task.owner";
    public const string Assignees = "task.assignees";
    public const string Tags = "task.tags";
    public const string Flags = "task.flags";
    public const string Comments = "task.comments";
    public const string Relations = "task.relations";
    public const string CreatedBy = "task.created_by";
    public const string CreatedAt = "task.created_at";
    public const string UpdatedAt = "task.updated_at";
}
