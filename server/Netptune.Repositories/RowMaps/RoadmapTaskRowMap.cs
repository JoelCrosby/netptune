using Netptune.Core.Enums;

// ReSharper disable InconsistentNaming

namespace Netptune.Repositories.RowMaps;

public sealed class RoadmapTaskRowMap
{
    public int Total_Count { get; init; }

    public int Task_Id { get; init; }

    public int Project_Scope_Id { get; init; }

    public required string Task_Name { get; init; }

    public int Project_Id { get; init; }

    public required string Project_Name { get; init; }

    public required string Project_Key { get; init; }

    public int Status_Id { get; init; }

    public required string Status_Name { get; init; }

    public required string Status_Key { get; init; }

    public string? Status_Color { get; init; }

    public StatusCategory Status_Category { get; init; }

    public TaskPriority? Priority { get; init; }

    public DateOnly? Start_Date { get; init; }

    public DateOnly? Due_Date { get; init; }

    public int? Sprint_Id { get; init; }

    public required string Assignees { get; init; }
}

public sealed class RoadmapAssigneeRowMap
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? PictureUrl { get; init; }
}
