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

    internal static List<ActivityEntry> Generate(List<ActivityLog> activityLogs)
    {
        var entries = new List<ActivityEntry>();

        var groups = activityLogs
            .Where(log => log.EntityId is not null)
            .GroupBy(log => new
            {
                log.EntityType,
                EntityId = log.EntityId!.Value,
                log.UserId,
                Bucket = MergeableTypes.Contains(log.Type)
                    ? log.OccurredAt.Ticks / Bucket.Ticks
                    : log.Id,
                Mergeable = MergeableTypes.Contains(log.Type),
            });

        foreach (var group in groups)
        {
            var logs = group.OrderBy(log => log.OccurredAt).ToList();
            var first = logs[0];
            var last = logs[^1];

            entries.Add(new ActivityEntry
            {
                WorkspaceId = first.WorkspaceId,
                Workspace = first.Workspace,
                EntityType = group.Key.EntityType,
                EntityId = group.Key.EntityId,
                UserId = group.Key.UserId,
                User = first.User,
                TaskId = first.TaskId,
                ActivityType = group.Key.Mergeable && logs.Select(log => log.Type).Distinct().Count() > 1
                    ? ActivityType.Modify
                    : first.Type,
                ChangedFields = group.Key.Mergeable
                    ? logs.Select(log => ToField(log.Type)).Where(field => field is not null).Select(field => field!).Distinct().ToList()
                    : [],
                Meta = BuildMeta(logs, group.Key.Mergeable),
                LastActivityLogId = last.Id,
                FirstOccurredAt = first.OccurredAt,
                LastOccurredAt = last.OccurredAt,
                RevisionCount = logs.Count,
                IsOpen = false,
                WindowExpiresAt = last.OccurredAt.Add(Bucket),
                NotifiedAt = last.OccurredAt,
                CreatedAt = first.OccurredAt,
                CreatedByUserId = first.UserId,
                OwnerId = first.UserId,
            });
        }

        return entries;
    }

    private static JsonDocument? BuildMeta(List<ActivityLog> logs, bool mergeable)
    {
        if (!mergeable) return logs[^1].Meta;

        var fields = logs
            .Select(log => ToField(log.Type))
            .Where(field => field is not null)
            .Distinct()
            .ToDictionary(field => field!, field => new { old = $"old {field}", @new = $"new {field}" });

        return fields.Count == 0
            ? null
            : JsonSerializer.SerializeToDocument(new { fields });
    }

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
