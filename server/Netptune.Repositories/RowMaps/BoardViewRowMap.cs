using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public class BoardViewRowMap
{
    public string Board_Name { get; set; } = null!;

    public string Board_Identifier { get; set; } = null!;

    public int? Task_Id { get; set; }

    public string Task_Name { get; set; } = null!;

    public ProjectTaskStatus Task_Status { get; set; }

    public bool Task_Is_Flagged { get; set; }

    public double Task_Sort_Order { get; set; }

    public int Project_Id { get; set; }

    public string Project_Scope_Id { get; set; } = null!;

    public int Workspace_Id { get; set; }

    public int Board_Group_Id { get; set; }

    public string Board_Group_Name { get; set; } = null!;

    public BoardGroupType Board_Group_Type { get; set; }

    public double Board_Group_Sort_Order { get; set; }

    public string Assignee_Firstname { get; set; } = null!;

    public string Assignee_Lastname { get; set; } = null!;

    public string Assignee_Picture_Url { get; set; } = null!;

    public string Assignee_Id { get; set; } = null!;

    public string? Tag { get; set; }
}

public class BoardViewMetaRowMap
{
    public string Workspace_Identifier { get; set; } = null!;

    public string Project_Key { get; set; } = null!;
}
