using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Notifications;

public static class NotificationLink
{
    public static string Build(string workspaceSlug, EntityType entityType, string? identifier)
    {
        var workspaceRoot = $"/{workspaceSlug}";

        if (string.IsNullOrWhiteSpace(identifier))
        {
            return workspaceRoot;
        }

        return entityType switch
        {
            EntityType.Task => $"{workspaceRoot}/tasks/{identifier}",
            EntityType.Board or EntityType.BoardGroup => $"{workspaceRoot}/boards/{identifier}",
            EntityType.Project => $"{workspaceRoot}/projects/{identifier}",
            EntityType.Sprint => $"{workspaceRoot}/sprints/{identifier}",
            EntityType.Status => $"{workspaceRoot}/settings/workspace/statuses/{identifier}",
            _ => workspaceRoot,
        };
    }
}
