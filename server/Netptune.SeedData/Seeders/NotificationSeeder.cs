using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class NotificationSeeder : ISeeder
{
    public int Phase => 3;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Notifications.AddRange(
            context.ActivityLogs.Take(20).Select((log, i) =>
            {
                var workspaceUsers = context.UsersFor(log.Workspace);

                return new Notification
                {
                    ActivityLog = log,
                    User = workspaceUsers[i % workspaceUsers.Count],
                    Workspace = log.Workspace,
                    IsRead = i % 3 == 0,
                    Link = $"/{log.Workspace.Slug}/tasks",
                    EntityType = log.EntityType,
                    ActivityType = log.Type,
                    CreatedByUserId = log.User.Id,
                    OwnerId = log.User.Id,
                };
            })
        );

        // Login user (prepended at index 0) is a member of every workspace, so give
        // them a populated notifications view with ~30 notifications of their own.
        var loginUser = context.Users[0];

        context.Notifications.AddRange(
            context.ActivityLogs.Take(30).Select((log, i) => new Notification
            {
                ActivityLog = log,
                User = loginUser,
                Workspace = log.Workspace,
                IsRead = i % 4 == 0,
                Link = $"/{log.Workspace.Slug}/tasks",
                EntityType = log.EntityType,
                ActivityType = log.Type,
                CreatedByUserId = log.User.Id,
                OwnerId = log.User.Id,
            })
        );

        await dbContext.Notifications.AddRangeAsync(context.Notifications, ct);
    }
}
