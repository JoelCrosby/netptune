using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;

namespace Netptune.Core.Relations;

public static class DefaultRelationTypes
{
    public static readonly IReadOnlyList<DefaultRelationTypeDefinition> All =
        WorkspaceSetupTemplateCatalog.Find(WorkspaceSetupTemplateCatalog.DefaultKey)!
            .RelationTypes
            .Select((definition, index) => new DefaultRelationTypeDefinition(
                definition.Name,
                definition.InverseName,
                definition.Key,
                definition.Color,
                definition.Category,
                index))
            .ToList();

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
