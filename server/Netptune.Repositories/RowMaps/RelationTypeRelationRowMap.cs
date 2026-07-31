using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public sealed class RelationTypeRelationRowMap
{
    public int Relation_Id { get; set; }

    public int Source_Task_Id { get; set; }

    public string Source_Task_Name { get; set; } = null!;

    public int Source_Task_Scope_Id { get; set; }

    public bool Source_Task_Is_Archived { get; set; }

    public string? Source_Task_Project_Key { get; set; }

    public string Source_Task_Status_Name { get; set; } = null!;

    public string? Source_Task_Status_Color { get; set; }

    public StatusCategory Source_Task_Status_Category { get; set; }

    public int Target_Task_Id { get; set; }

    public string Target_Task_Name { get; set; } = null!;

    public int Target_Task_Scope_Id { get; set; }

    public bool Target_Task_Is_Archived { get; set; }

    public string? Target_Task_Project_Key { get; set; }

    public string Target_Task_Status_Name { get; set; } = null!;

    public string? Target_Task_Status_Color { get; set; }

    public StatusCategory Target_Task_Status_Category { get; set; }

    public int Total_Count { get; set; }
}
