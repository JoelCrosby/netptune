namespace Netptune.Core.Events.Relations;

public class TaskRelationActivityMeta
{
    public int RelationTypeId { get; init; }

    public string RelationTypeName { get; init; } = null!;

    public string Label { get; init; } = null!;

    public int RelatedTaskId { get; init; }

    public string RelatedTaskSystemId { get; init; } = null!;

    public string RelatedTaskName { get; init; } = null!;
}
