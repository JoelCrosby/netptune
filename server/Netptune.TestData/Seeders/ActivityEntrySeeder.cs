using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class ActivityEntrySeeder
{
    private static readonly ActivityType[] MergeableTypes =
    [
        ActivityType.Modify,
        ActivityType.ModifyName,
        ActivityType.ModifyDescription,
        ActivityType.ModifyStatus,
        ActivityType.ModifyPriority,
        ActivityType.ModifyEstimate,
        ActivityType.ModifyDueDate,
    ];

    private static readonly TimeSpan Bucket = TimeSpan.FromMinutes(5);

    internal static List<ActivityEntry> Generate(List<EventRecord> activityLogs, List<AppUser> users, List<Workspace> workspaces)
    {
        var entries = new List<ActivityEntry>();

        var groups = activityLogs
            .Where(log => log.SubjectId is not null && log.ActorUserId is not null)
            .GroupBy(log => new
            {
                EntityType = Enum.Parse<EntityType>(log.SubjectType!, true),
                EntityId = int.Parse(log.SubjectId!),
                UserId = log.ActorUserId!,
                Bucket = MergeableTypes.Contains((ActivityType)log.Payload.RootElement.GetProperty("activityType").GetInt32())
                    ? log.OccurredAt.Ticks / Bucket.Ticks
                    : log.Id,
                Mergeable = MergeableTypes.Contains((ActivityType)log.Payload.RootElement.GetProperty("activityType").GetInt32()),
            });

        foreach (var group in groups)
        {
            var logs = group.OrderBy(log => log.OccurredAt).ToList();
            var first = logs[0];
            var last = logs[^1];

            entries.Add(new ActivityEntry
            {
                WorkspaceId = first.WorkspaceId!.Value,
                Workspace = workspaces.Single(workspace => workspace.Id == first.WorkspaceId),
                EntityType = group.Key.EntityType,
                EntityId = group.Key.EntityId,
                UserId = group.Key.UserId,
                User = users.Single(user => user.Id == group.Key.UserId),
                TaskId = group.Key.EntityType == EntityType.Task ? group.Key.EntityId : null,
                ActivityType = group.Key.Mergeable && logs.Select(TypeOf).Distinct().Count() > 1
                    ? ActivityType.Modify
                    : TypeOf(first),
                ChangedFields = group.Key.Mergeable
                    ? logs.Select(log => ToField(TypeOf(log))).Where(field => field is not null).Select(field => field!).Distinct().ToList()
                    : [],
                Meta = BuildMeta(logs, group.Key.Mergeable),
                LastEventRecordId = last.Id,
                FirstOccurredAt = first.OccurredAt,
                LastOccurredAt = last.OccurredAt,
                RevisionCount = logs.Count,
                IsOpen = false,
                WindowExpiresAt = last.OccurredAt.Add(Bucket),
                NotifiedAt = last.OccurredAt,
                CreatedAt = first.OccurredAt,
                CreatedByUserId = first.ActorUserId!,
                OwnerId = first.ActorUserId!,
            });
        }

        return entries;
    }

    private static JsonDocument? BuildMeta(List<EventRecord> logs, bool mergeable)
    {

        if (!mergeable)
        {
            return logs[^1].Payload;
        }

        var fields = logs
            .Select(log => ToField(TypeOf(log)))
            .Where(field => field is not null)
            .Distinct()
            .ToDictionary(field => field!, field => new { old = $"old {field}", @new = $"new {field}" });

        return fields.Count == 0
            ? null
            : JsonSerializer.SerializeToDocument(new { fields });
    }

    private static ActivityType TypeOf(EventRecord record) =>
        (ActivityType)record.Payload.RootElement.GetProperty("activityType").GetInt32();

    private static string? ToField(ActivityType type) => type switch
    {
        ActivityType.ModifyName => "name",
        ActivityType.ModifyDescription => "description",
        ActivityType.ModifyStatus => "status",
        ActivityType.ModifyPriority => "priority",
        ActivityType.ModifyEstimate => "estimate",
        ActivityType.ModifyDueDate => "dueDate",
        _ => null,
    };
}
