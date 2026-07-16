using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;

namespace Netptune.Core.Statuses;

public static class DefaultTaskStatuses
{
    public static readonly IReadOnlyList<DefaultTaskStatusDefinition> All =
        WorkspaceSetupTemplateCatalog.Find(WorkspaceSetupTemplateCatalog.DefaultKey)!
            .Statuses
            .Select((definition, index) => new DefaultTaskStatusDefinition(
                definition.Name,
                definition.Key,
                definition.Color,
                definition.Category,
                index))
            .ToList();

    public static Status Create(DefaultTaskStatusDefinition definition, int workspaceId, string? ownerId)
    {
        return new Status
        {
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            EntityType = EntityType.Task,
            Name = definition.Name,
            Key = definition.Key,
            Color = definition.Color,
            Category = definition.Category,
            SortOrder = definition.SortOrder,
            IsSystem = true,
        };
    }
}

public sealed record DefaultTaskStatusDefinition(
    string Name,
    string Key,
    string Color,
    StatusCategory Category,
    double SortOrder);
