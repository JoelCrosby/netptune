using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Statuses;

public static class DefaultTaskStatuses
{
    public static readonly IReadOnlyList<DefaultTaskStatusDefinition> All =
    [
        new("New", "new", "#6b7280", StatusCategory.Todo, 0),
        new("In Progress", "in-progress", "#2563eb", StatusCategory.Active, 1),
        new("On Hold", "on-hold", "#f59e0b", StatusCategory.Backlog, 2),
        new("Un-assigned", "un-assigned", "#8b5cf6", StatusCategory.Backlog, 3),
        new("Blocked", "blocked", "#b81414", StatusCategory.Inactive, 4),
        new("Inactive", "inactive", "#64748b", StatusCategory.Inactive, 5),
        new("Complete", "complete", "#16a34a", StatusCategory.Done, 6),
    ];

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
