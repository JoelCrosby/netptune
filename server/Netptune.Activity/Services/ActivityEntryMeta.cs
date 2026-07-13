using System.Text.Json;
using System.Text.Json.Nodes;

using Netptune.Core.Events;

namespace Netptune.Activity.Services;

// The entry's meta jsonb. Written here, merged by upsert_activity_entry.sql and read back by IsNoOpBurst —
// all three must agree on this shape:
//
//   { "fields": { "description": { "old": …, "new": …, "oldHash": …, "newHash": … } },
//     "ipAddress": …, "userAgent": … }
internal static class ActivityEntryMeta
{
    private const string FieldsKey = "fields";
    private const string OldKey = "old";
    private const string NewKey = "new";
    private const string OldHashKey = "oldHash";
    private const string NewHashKey = "newHash";

    public static string Build(IReadOnlyList<ActivityEvent> eventsOldestFirst)
    {
        var fields = new JsonObject();

        foreach (var activity in eventsOldestFirst)
        {
            if (activity.Field is not { } field) continue;

            var name = FieldName(field);

            if (fields[name] is JsonObject existing)
            {
                existing[NewKey] = JsonValue.Create(activity.NewValue);
                existing[NewHashKey] = JsonValue.Create(activity.NewValueHash);

                continue;
            }

            fields[name] = new JsonObject
            {
                [OldKey] = JsonValue.Create(activity.OldValue),
                [OldHashKey] = JsonValue.Create(activity.OldValueHash),
                [NewKey] = JsonValue.Create(activity.NewValue),
                [NewHashKey] = JsonValue.Create(activity.NewValueHash),
            };
        }

        var meta = new JsonObject
        {
            [FieldsKey] = fields,
        };

        var last = eventsOldestFirst[^1];

        if (last.IpAddress is not null) meta["ipAddress"] = JsonValue.Create(last.IpAddress);
        if (last.UserAgent is not null) meta["userAgent"] = JsonValue.Create(last.UserAgent);

        return meta.ToJsonString();
    }

    public static List<string> ChangedFields(IReadOnlyList<ActivityEvent> events)
    {
        var fields = new List<string>();

        foreach (var activity in events)
        {
            if (activity.Field is not { } field) continue;

            var name = FieldName(field);

            if (!fields.Contains(name)) fields.Add(name);
        }

        return fields;
    }

    public static bool IsNoOpBurst(JsonDocument? meta)
    {
        if (meta is null) return false;
        if (meta.RootElement.ValueKind is not JsonValueKind.Object) return false;
        if (!meta.RootElement.TryGetProperty(FieldsKey, out var fields)) return false;
        if (fields.ValueKind is not JsonValueKind.Object) return false;

        var any = false;

        foreach (var field in fields.EnumerateObject())
        {
            any = true;

            if (!Unchanged(field.Value)) return false;
        }

        return any;
    }

    private static bool Unchanged(JsonElement field)
    {
        if (field.ValueKind is not JsonValueKind.Object) return false;

        var oldHash = ReadString(field, OldHashKey);
        var newHash = ReadString(field, NewHashKey);

        // A hash is present only when the value was too long to store whole, so the stored values are
        // prefixes and comparing them would read a real edit past character 256 as an undo.
        if (oldHash is not null || newHash is not null)
        {
            return oldHash is not null && oldHash == newHash;
        }

        return ReadString(field, OldKey) == ReadString(field, NewKey);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string FieldName(Core.Enums.TaskChangeField field)
    {
        var name = field.ToString();

        return string.Concat(char.ToLowerInvariant(name[0]), name[1..]);
    }
}
