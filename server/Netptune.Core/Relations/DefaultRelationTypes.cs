using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Relations;

public static class DefaultRelationTypes
{
    public static readonly IReadOnlyList<DefaultRelationTypeDefinition> All =
    [
        new("Parent of", "Child of", "parent-of", "#8b5cf6", RelationCategory.Hierarchy, 0),
        new("Blocks", "Is Blocked By", "blocks", "#b81414", RelationCategory.Dependency, 1),
        new("Relates To", "Relates To", "relates-to", "#6b7280", RelationCategory.Related, 2),
        new("Duplicates", "Is Duplicated By", "duplicates", "#f59e0b", RelationCategory.Duplicate, 3),
    ];

    public static RelationType Create(DefaultRelationTypeDefinition definition, int workspaceId, string? ownerId)
    {
        return new RelationType
        {
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            Name = definition.Name,
            InverseName = definition.InverseName,
            Key = definition.Key,
            Color = definition.Color,
            Category = definition.Category,
            SortOrder = definition.SortOrder,
            IsSystem = true,
        };
    }
}

public sealed record DefaultRelationTypeDefinition(
    string Name,
    string InverseName,
    string Key,
    string Color,
    RelationCategory Category,
    double SortOrder);
