using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public class BoardViewRowMap
{
    public int? Task_Id { get; set; }

    public string Task_Name { get; set; } = null!;

    public int Task_Status_Id { get; set; }

    public string Task_Status_Name { get; set; } = null!;

    public string Task_Status_Key { get; set; } = null!;

    public string? Task_Status_Color { get; set; }

    public StatusCategory Task_Status_Category { get; set; }

    public TaskPriority? Task_Priority { get; set; }

    public EstimateType? Task_Estimate_Type { get; set; }

    public decimal? Task_Estimate_Value { get; set; }

    public DateOnly? Task_Due_Date { get; set; }

    public DateTime Task_Created_At { get; set; }

    public DateTime Task_Updated_At { get; set; }

    public int? Sprint_Id { get; set; }

    public string? Sprint_Name { get; set; }

    public SprintStatus? Sprint_Status { get; set; }

    public double Task_Sort_Order { get; set; }

    public int Project_Id { get; set; }

    public string Project_Scope_Id { get; set; } = null!;

    public int Workspace_Id { get; set; }

    public int Board_Group_Id { get; set; }

    public string Board_Group_Name { get; set; } = null!;

    public int? Board_Group_Status_Id { get; set; }

    public double Board_Group_Sort_Order { get; set; }

    public string[] Tags { get; set; } = [];

    public bool Has_Comments { get; set; }

    public string Assignees { get; set; } = "[]";
}

public class BoardViewAssigneeRowMap
{
    public string Id { get; set; } = null!;

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string? Picture_Url { get; set; }
}

public class BoardViewMetaRowMap
{
    public string Workspace_Identifier { get; set; } = null!;

    public string Project_Key { get; set; } = null!;
}
