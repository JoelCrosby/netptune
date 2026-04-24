using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class ActivityLogSeeder
{
    private static readonly ActivityType[] Types = Enum.GetValues<ActivityType>();
    private static readonly DateTime BaseDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static List<ActivityLog> Generate(List<ProjectTask> tasks, List<AppUser> users, List<Workspace> workspaces) =>
        tasks
            .SelectMany((task, ti) => Enumerable.Range(0, 32).Select(i =>
            {
                var idx = ti * 32 + i;
                return new ActivityLog
                {
                    EntityType = EntityType.Task,
                    TaskId = task.Id,
                    EntityId = task.Id,
                    OccurredAt = BaseDate.AddHours(idx),
                    Type = Types[idx % Types.Length],
                    User = users[idx % users.Count],
                    Workspace = workspaces[idx % workspaces.Count],
                };
            }))
            .ToList();
}
