using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;

namespace Netptune.TestData.Seeders;

internal static class NotificationSeeder
{
    internal static List<Notification> Generate(
        List<EventRecord> activityLogs,
        List<Workspace> workspaces,
        List<WorkspaceAppUser> workspaceUsers)
    {
        var workspace = workspaces.First(w => w.Slug == "netptune");
        var user = workspaceUsers.First(wu => wu.Workspace == workspace).User;

        return activityLogs.Take(20).Select((log, i) => new Notification
        {
            User = user,
            EventRecord = log,
            Workspace = workspace,
            IsRead = i % 3 == 0,
            Link = $"/{workspace.Slug}/tasks",
            EntityType = Enum.Parse<EntityType>(log.SubjectType!, true),
            ActivityType = (ActivityType)log.Payload.RootElement.GetProperty("activityType").GetInt32(),
            CreatedByUserId = log.ActorUserId!,
            OwnerId = log.ActorUserId!,
        }).ToList();
    }
}
