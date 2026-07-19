using Netptune.Core.Enums;

// ReSharper disable InconsistentNaming

namespace Netptune.Repositories.RowMaps;

public sealed class RoadmapRelationRowMap
{
    public int Id { get; init; }

    public int Source_Task_Id { get; init; }

    public int Target_Task_Id { get; init; }

    public int Relation_Type_Id { get; init; }

    public required string Relation_Type_Key { get; init; }

    public RelationCategory Category { get; init; }
}
