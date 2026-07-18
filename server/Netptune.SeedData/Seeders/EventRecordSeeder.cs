using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class EventRecordSeeder : ISeeder
{
    public int Phase => 2;

    private static readonly ActivityType[] Types = Enum.GetValues<ActivityType>();
    private static readonly DateTime BaseDate = new(
        2025,
        1,
        6,
        9,
        0,
        0,
        DateTimeKind.Utc);

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.EventRecords.AddRange(
            context.Tasks.SelectMany((task, ti) =>
            {
                var workspaceUsers = context.UsersFor(task.Workspace);

                return Enumerable.Range(0, 4).Select(i =>
                {
                    var idx = ti * 4 + i;

                    return new EventRecord
                    {
                        EventId = Guid.NewGuid(),
                        WorkspaceId = task.WorkspaceId,
                        EventKey = EventKeys.EntityActivityRecorded,
                        SubjectType = EventEntityTypes.From(EntityType.Task),
                        SubjectId = task.Id.ToString(),
                        OccurredAt = BaseDate.AddHours(idx),
                        RecordedAt = BaseDate.AddHours(idx),
                        ActorUserId = workspaceUsers[idx % workspaceUsers.Count].Id,
                        RetentionClass = EventRetentionClasses.Audit,
                        Payload = JsonSerializer.SerializeToDocument(new
                        {
                            activityType = (int)Types[idx % Types.Length],
                            taskId = task.Id,
                        }),
                    };
                });
            })
        );

        await dbContext.EventRecords.AddRangeAsync(context.EventRecords, ct);
    }
}
