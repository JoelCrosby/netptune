using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public sealed class TaskRelationRowMap
{
    public int Relation_Id { get; set; }

    public int Relation_Type_Id { get; set; }

    public bool Is_Source { get; set; }

    public string Relation_Type_Name { get; set; } = null!;

    public string Relation_Type_Inverse_Name { get; set; } = null!;

    public string Relation_Type_Key { get; set; } = null!;

    public string? Relation_Type_Color { get; set; }

    public RelationCategory Relation_Type_Category { get; set; }

    public double Relation_Type_Sort_Order { get; set; }

    public int Other_Task_Id { get; set; }

    public string Other_Task_Name { get; set; } = null!;

    public int Other_Task_Scope_Id { get; set; }

    public string? Other_Task_Project_Key { get; set; }

    public string Other_Task_Status_Name { get; set; } = null!;

    public string? Other_Task_Status_Color { get; set; }

    public StatusCategory Other_Task_Status_Category { get; set; }
}
