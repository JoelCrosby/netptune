using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Notifications;

public static class NotificationLink
{
    public static string Build(string workspaceSlug, EntityType entityType, ActivityType activityType, string? identifier)
    {
        var workspaceRoot = $"/{workspaceSlug}";

        var isTransferJob = activityType is ActivityType.ImportCompleted
            or ActivityType.ImportFailed
            or ActivityType.ExportCompleted
            or ActivityType.ExportFailed;

        if (isTransferJob)
        {
            return $"{workspaceRoot}/settings/workspace/data";
        }

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
