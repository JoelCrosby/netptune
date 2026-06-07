using System.Text.Json;

namespace Netptune.Automation.Configuration;

internal static class ConfigReader
{
    public static TEnum? ReadEnum<TEnum>(JsonDocument? document, string property)
        where TEnum : struct, Enum
    {
        if (document is null || !document.RootElement.TryGetProperty(property, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intValue))
        {
            return Enum.IsDefined(typeof(TEnum), intValue) ? (TEnum)(object)intValue : null;
        }

        if (element.ValueKind == JsonValueKind.String && Enum.TryParse<TEnum>(element.GetString(), true, out var enumValue))
        {
            return enumValue;
        }

        return null;
    }

    public static int? ReadInt(JsonDocument? document, string property)
    {
        return document is not null
               && document.RootElement.TryGetProperty(property, out var element)
               && element.TryGetInt32(out var value)
            ? value
            : null;
    }

    public static string? ReadString(JsonDocument? document, string property)
    {
        return document is not null
               && document.RootElement.TryGetProperty(property, out var element)
               && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}
