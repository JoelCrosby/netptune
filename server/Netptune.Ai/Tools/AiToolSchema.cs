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

        var isNumber = value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed);

        return isNumber ? value.GetInt32() : null;
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
