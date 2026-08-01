using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution.Handlers;

public static class AiChangePayload
{
    public static string? ReadString(JsonElement payload, string name)
    {
        var hasProperty = payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String;

        return hasProperty ? payload.GetProperty(name).GetString() : null;
    }

    public static int? ReadInt(JsonElement payload, string name)
    {
        var isObject = payload.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasProperty = payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number;

        return hasProperty ? value.GetInt32() : null;
    }

    public static List<string> ReadStringArray(JsonElement payload, string name)
    {
        var isObject = payload.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    public static List<int> ReadIntArray(JsonElement payload, string name)
    {
        var isObject = payload.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number)
            .Select(item => item.GetInt32())
            .ToList();
    }

    public static DateOnly? ReadDate(JsonElement payload, string name)
    {
        var raw = ReadString(payload, name);
        var isParsed = DateOnly.TryParse(raw, out var parsed);

        return isParsed ? parsed : null;
    }

    public static int? ResolveTaskId(AiChangeApplyContext context)
    {
        var change = context.Change;

        if (change.EntityId.HasValue)
        {
            return change.EntityId;
        }

        var payload = change.Payload.RootElement;
        var refKey = ReadString(payload, "taskRef");
        var hasRef = refKey is not null && context.ResolvedRefs.ContainsKey(refKey);

        if (hasRef)
        {
            return context.ResolvedRefs[refKey!];
        }

        return ReadInt(payload, "taskId");
    }

    public static AiAppliedChangeResult Failure(AiProposedChange change, string message)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Failed,
            Error = message,
        };
    }

    public static AiAppliedChangeResult Applied(AiProposedChange change, int? entityId)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Applied,
            AppliedEntityId = entityId,
        };
    }
}
