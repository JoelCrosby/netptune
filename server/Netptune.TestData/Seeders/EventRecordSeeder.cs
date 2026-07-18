using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.TestData.Seeders;

internal static class EventRecordSeeder
{
    private static readonly ActivityType[] Types = Enum.GetValues<ActivityType>();
    private static readonly DateTime BaseDate = new(
        2025,
        1,
        1,
        0,
        0,
        0,
        DateTimeKind.Utc);

    internal static List<EventRecord> Generate(List<ProjectTask> tasks, List<AppUser> users, List<Workspace> workspaces) =>
        tasks
            .SelectMany((task, ti) => Enumerable.Range(0, 32).Select(i =>
            {
                var idx = ti * 32 + i;

                return new EventRecord
                {
                    EventId = Guid.NewGuid(),
                    WorkspaceId = task.WorkspaceId,
                    EventKey = EventKeys.EntityActivityRecorded,
                    SubjectType = EventEntityTypes.From(EntityType.Task),
                    SubjectId = task.Id.ToString(),
                    OccurredAt = BaseDate.AddHours(idx),
                    RecordedAt = BaseDate.AddHours(idx),
                    ActorUserId = users[idx % users.Count].Id,
                    RetentionClass = EventRetentionClasses.Audit,
                    Payload = JsonSerializer.SerializeToDocument(new
                    {
                        activityType = (int)Types[idx % Types.Length],
                        taskId = task.Id,
                    }),
                };
            }))
            .ToList();
}
