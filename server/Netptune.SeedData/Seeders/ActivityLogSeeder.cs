using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class ActivityLogSeeder : ISeeder
{
    public int Phase => 2;

    private static readonly ActivityType[] Types = Enum.GetValues<ActivityType>();
    private static readonly DateTime BaseDate = new(2025, 1, 6, 9, 0, 0, DateTimeKind.Utc);

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.ActivityLogs.AddRange(
            context.Tasks.SelectMany((task, ti) =>
            {
                var workspaceUsers = context.UsersFor(task.Workspace);

                return Enumerable.Range(0, 4).Select(i =>
                {
                    var idx = ti * 4 + i;

                    return new ActivityLog
                    {
                        EntityType = EntityType.Task,
                        TaskId = task.Id,
                        EntityId = task.Id,
                        OccurredAt = BaseDate.AddHours(idx),
                        Type = Types[idx % Types.Length],
                        User = workspaceUsers[idx % workspaceUsers.Count],
                        Workspace = task.Workspace,
                        WorkspaceSlug = task.Workspace.Slug,
                    };
                });
            })
        );

        await dbContext.ActivityLogs.AddRangeAsync(context.ActivityLogs, ct);
    }
}
