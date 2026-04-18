using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.TestData.Seeders;

internal static class NotificationSeeder
{
    internal static List<Notification> Generate(
        List<ActivityLog> activityLogs,
        List<Workspace> workspaces,
        List<WorkspaceAppUser> workspaceUsers)
    {
        var workspace = workspaces.First(w => w.Slug == "netptune");
        var user = workspaceUsers.First(wu => wu.Workspace == workspace).User;

        return activityLogs.Take(20).Select((log, i) => new Notification
        {
            User = user,
            ActivityLog = log,
            Workspace = workspace,
            IsRead = i % 3 == 0,
            Link = $"/{workspace.Slug}/tasks",
            EntityType = log.EntityType,
            ActivityType = log.Type,
            CreatedByUserId = log.UserId,
            OwnerId = log.UserId,
        }).ToList();
    }
}
