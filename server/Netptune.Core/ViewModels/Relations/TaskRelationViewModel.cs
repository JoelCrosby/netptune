using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.ViewModels.Relations;

public sealed record TaskRelationViewModel
{
    public int Id { get; init; }

    public int RelationTypeId { get; init; }

    public string RelationTypeName { get; init; } = null!;

    public string RelationTypeKey { get; init; } = null!;

    public string? RelationTypeColor { get; init; }

    public RelationCategory RelationTypeCategory { get; init; }

    public string Label { get; init; } = null!;

    public bool IsSource { get; init; }

    public RelatedTaskViewModel RelatedTask { get; init; } = null!;

    public static TaskRelationViewModel BuildView(int relationId, RelationType relationType, bool isSource, TaskViewModel relatedTask)
    {
        return new TaskRelationViewModel
        {
            Id = relationId,
            RelationTypeId = relationType.Id,
            RelationTypeName = relationType.Name,
            RelationTypeKey = relationType.Key,
            RelationTypeColor = relationType.Color,
            RelationTypeCategory = relationType.Category,
            Label = isSource ? relationType.Name : relationType.InverseName,
            IsSource = isSource,
            RelatedTask = new RelatedTaskViewModel
            {
                Id = relatedTask.Id,
                SystemId = relatedTask.SystemId,
                Name = relatedTask.Name,
                StatusName = relatedTask.StatusName,
                StatusColor = relatedTask.StatusColor,
                StatusCategory = relatedTask.StatusCategory,
            },
        };
    }
}

public sealed record RelatedTaskViewModel
{
    public int Id { get; init; }

    public string SystemId { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string StatusName { get; init; } = null!;

    public string? StatusColor { get; init; }

    public StatusCategory StatusCategory { get; init; }
}
