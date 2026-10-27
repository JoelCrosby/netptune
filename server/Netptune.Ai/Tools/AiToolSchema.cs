using System.Globalization;
using System.Text.Json;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public static class AiToolSchema
{
    public static JsonDocument Object(string propertiesJson, params string[] required)
    {
        var requiredJson = JsonSerializer.Serialize(required);

        return JsonDocument.Parse(
            $$"""
            {
              "type": "object",
              "properties": {{propertiesJson}},
              "required": {{requiredJson}},
              "additionalProperties": false
            }
            """);
    }

    public static JsonDocument Empty()
    {
        return Object("{}");
    }

    // Only a value the tool knows is named, so the work log never repeats whatever the model sent.
    public static string DescribeCall(
        string toolName,
        JsonElement arguments,
        string argument,
        IEnumerable<string> knownValues)
    {
        var value = GetString(arguments, argument);
        var isKnown = value is not null && knownValues.Contains(value, StringComparer.Ordinal);

        return isKnown ? $"{toolName} · {value}" : toolName;
    }

    public static string? GetString(JsonElement arguments, string name)
    {
        var hasProperty = arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String;

        if (!hasProperty)
        {
            return null;
        }

        return arguments.GetProperty(name).GetString();
    }

    public static int? GetInt(JsonElement arguments, string name)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasProperty = arguments.TryGetProperty(name, out var value);

        if (!hasProperty)
        {
            return null;
        }

        var isNumber = value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _);

        return isNumber ? value.GetInt32() : null;
    }

    public static decimal? GetDecimal(JsonElement arguments, string name)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasProperty = arguments.TryGetProperty(name, out var value);

        if (!hasProperty)
        {
            return null;
        }

        var isNumber = value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _);

        return isNumber ? value.GetDecimal() : null;
    }

    public static DateTime? GetDate(JsonElement arguments, string name)
    {
        var value = GetString(arguments, name);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parsed = DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var date);

        return parsed ? date : null;
    }

    // A model can ask for any page number, so the arithmetic runs in long rather than wrapping round.
    public static int Skip(int page, int pageSize)
    {
        var skip = (long)(page - 1) * pageSize;

        return (int)Math.Clamp(skip, 0, int.MaxValue);
    }

    public static bool HasMore(int page, int pageSize, long totalCount)
    {
        return (long)page * pageSize < totalCount;
    }

    public static TEnum? GetEnum<TEnum>(JsonElement arguments, string name)
        where TEnum : struct, Enum
    {
        var value = GetString(arguments, name);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parsed = TryParseEnum<TEnum>(value, out var result);

        return parsed ? result : null;
    }

    // Enum.TryParse also takes numbers and comma lists, so "99" would become an undefined value, and "1" or
    // "High, Low" a silent guess at what the model meant. Only a single defined name is accepted.
    public static bool TryParseEnum<TEnum>(string? raw, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;

        var trimmed = raw?.Trim();
        var isName = !string.IsNullOrEmpty(trimmed)
            && char.IsLetter(trimmed[0])
            && trimmed.All(char.IsLetterOrDigit);

        return isName && Enum.TryParse(trimmed, true, out value) && Enum.IsDefined(value);
    }

    public static bool? GetBool(JsonElement arguments, string name)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasProperty = arguments.TryGetProperty(name, out var value);

        if (!hasProperty)
        {
            return null;
        }

        var isBoolean = value.ValueKind is JsonValueKind.True or JsonValueKind.False;

        return isBoolean ? value.GetBoolean() : null;
    }

    public static List<int> GetIntArray(JsonElement arguments, string name)
    {
        var items = GetArray(arguments, name);

        return items
            .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out _))
            .Select(item => item.GetInt32())
            .ToList();
    }

    public static List<string> GetStringArray(JsonElement arguments, string name)
    {
        var items = GetArray(arguments, name);

        return items
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!.Trim())
            .Where(item => item.Length > 0)
            .ToList();
    }

    private static List<JsonElement> GetArray(JsonElement arguments, string name)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasArray = arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array;

        return hasArray ? value.EnumerateArray().ToList() : [];
    }

    public static void AddOptionalField(List<AiChangeField> fields, string name, string? value)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (!hasValue)
        {
            return;
        }

        fields.Add(new AiChangeField { Name = name, After = value });
    }
}
