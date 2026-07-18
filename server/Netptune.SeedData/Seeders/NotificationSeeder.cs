using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class NotificationSeeder : ISeeder
{
    public int Phase => 3;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Notifications.AddRange(
            context.EventRecords.Take(20).Select((log, i) =>
            {
                var workspace = context.Workspaces.Single(item => item.Id == log.WorkspaceId);
                var workspaceUsers = context.UsersFor(workspace);
                var actor = context.Users.Single(user => user.Id == log.ActorUserId);

                return new Notification
                {
                    EventRecord = log,
                    User = workspaceUsers[i % workspaceUsers.Count],
                    Workspace = workspace,
                    IsRead = i % 3 == 0,
                    Link = $"/{workspace.Slug}/tasks",
                    EntityType = Enum.Parse<EntityType>(log.SubjectType!, true),
                    ActivityType = (ActivityType)log.Payload.RootElement.GetProperty("activityType").GetInt32(),
                    CreatedByUserId = actor.Id,
                    OwnerId = actor.Id,
                };
            })
        );

        // Login user (prepended at index 0) is a member of every workspace, so give
        // them a populated notifications view with ~30 notifications of their own.
        var loginUser = context.Users[0];

        context.Notifications.AddRange(
            context.EventRecords.Take(30).Select((log, i) =>
            {
                var workspace = context.Workspaces.Single(item => item.Id == log.WorkspaceId);
                var actor = context.Users.Single(user => user.Id == log.ActorUserId);

                return new Notification
                {
                    EventRecord = log,
                    User = loginUser,
                    Workspace = workspace,
                    IsRead = i % 4 == 0,
                    Link = $"/{workspace.Slug}/tasks",
                    EntityType = Enum.Parse<EntityType>(log.SubjectType!, true),
                    ActivityType = (ActivityType)log.Payload.RootElement.GetProperty("activityType").GetInt32(),
                    CreatedByUserId = actor.Id,
                    OwnerId = actor.Id,
                };
            })
        );

        await dbContext.Notifications.AddRangeAsync(context.Notifications, ct);
    }
}
