using Netptune.Core.Enums;

// ReSharper disable InconsistentNaming

namespace Netptune.Repositories.RowMaps;

public sealed class TaskViewRowMap
{
    public int Total_Count { get; init; }

    public int Task_Id { get; init; }

    public string Owner_Id { get; init; } = null!;

    public string Task_Name { get; init; } = null!;

    public string? Task_Description { get; init; }

    public int Task_Status_Id { get; init; }

    public string Task_Status_Name { get; init; } = null!;

    public string Task_Status_Key { get; init; } = null!;

    public string? Task_Status_Color { get; init; }

    public StatusCategory Task_Status_Category { get; init; }

    public int Project_Scope_Id { get; init; }

    public TaskPriority? Task_Priority { get; init; }

    public EstimateType? Task_Estimate_Type { get; init; }

    public decimal? Task_Estimate_Value { get; init; }

    public int? Project_Id { get; init; }

    public int? Sprint_Id { get; init; }

    public string? Sprint_Name { get; init; }

    public SprintStatus? Sprint_Status { get; init; }

    public int Workspace_Id { get; init; }

    public string Workspace_Key { get; init; } = null!;

    public DateTime Task_Created_At { get; init; }

    public DateTime? Task_Updated_At { get; init; }

    public string Owner_Username { get; init; } = null!;

    public string? Owner_Picture_Url { get; init; }

    public string? Project_Key { get; init; }

    public string? Project_Name { get; init; }

    public string? Tag { get; init; }

    public string? Assignee_Id { get; init; }

    public string? Assignee_Firstname { get; init; }

    public string? Assignee_Lastname { get; init; }

    public string? Assignee_Picture_Url { get; init; }
}
