using System;
using Netptune.Core.Enums;

// ReSharper disable InconsistentNaming

namespace Netptune.Repositories.RowMaps;

public class TasksViewRowMap
{
    public string Board_Name { get; set; } = null!;

    public string Board_Identifier { get; set; } = null!;

    public string Task_Description { get; set; } = null!;

    public int Task_Id { get; set; }

    public string Task_Name { get; set; } = null!;

    public ProjectTaskStatus Task_Status { get; set; }

    public bool Task_Is_Flagged { get; set; }

    public double Task_Sort_Order { get; set; }

    public string Project_Scope_Id { get; set; } = null!;

    public string Board_Group_Name { get; set; } = null!;

    public BoardGroupType Board_Group_Type { get; set; }

    public double Board_Group_Sort_Order { get; set; }

    public string? Assignee_Firstname { get; set; } = null!;

    public string? Assignee_Lastname { get; set; } = null!;

    public string? Assignee_Email { get; set; } = null!;

    public string Owner_Firstname { get; set; } = null!;

    public string Owner_Lastname { get; set; } = null!;

    public string Owner_Email { get; set; } = null!;

    public string? Tag { get; set; } = null!;

    public DateTime Task_Created_At { get; set; }

    public DateTime? Task_Updated_At { get; set; }

    public string Workspace_Key { get; set; } = null!;

    public string Workspace_Identifier { get; set; } = null!;

    public string Project_Key { get; set; } = null!;

    public string Project_Name { get; set; } = null!;
}
