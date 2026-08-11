using Netptune.Core.ViewModels.Relations;

namespace Netptune.Core.Events.Relations;

public class TaskRelationActivityMeta
{
    public int RelationTypeId { get; init; }

    public string RelationTypeName { get; init; } = null!;

    public string Label { get; init; } = null!;

    public int RelatedTaskId { get; init; }

    public string RelatedTaskSystemId { get; init; } = null!;

    public string RelatedTaskName { get; init; } = null!;

    public static TaskRelationActivityMeta From(TaskRelationViewModel view)
    {
        return new TaskRelationActivityMeta
        {
            RelationTypeId = view.RelationTypeId,
            RelationTypeName = view.RelationTypeName,
            Label = view.Label,
            RelatedTaskId = view.RelatedTask.Id,
            RelatedTaskSystemId = view.RelatedTask.SystemId,
            RelatedTaskName = view.RelatedTask.Name,
        };
    }
}
