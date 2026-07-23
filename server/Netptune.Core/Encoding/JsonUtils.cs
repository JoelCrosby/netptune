using System.Text.Json;

namespace Netptune.Core.Encoding;

public static class JsonUtils
{
    public static TEnum? ReadEnum<TEnum>(JsonDocument? document, string property) where TEnum : struct, Enum
    {
        if (document is null)
        {
            return null;
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return null;
        }

        var isNumber = element.ValueKind == JsonValueKind.Number;

        if (isNumber)
        {
            var hasIntegerValue = element.TryGetInt32(out var intValue);

            if (hasIntegerValue)
            {
                var isDefinedValue = Enum.IsDefined(typeof(TEnum), intValue);

                if (isDefinedValue)
                {
                    return (TEnum)(object)intValue;
                }
            }
        }

        var isString = element.ValueKind == JsonValueKind.String;

        if (!isString)
        {
            return null;
        }

        var text = element.GetString();
        var hasEnumValue = Enum.TryParse<TEnum>(text, true, out var enumValue);

        if (!hasEnumValue)
        {
            return null;
        }

        return enumValue;
    }

    public static int? ReadInt(JsonDocument? document, string property)
    {
        if (document is null)
        {
            return null;
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return null;
        }

        var isNumber = element.ValueKind == JsonValueKind.Number;

        if (!isNumber)
        {
            return null;
        }

        var hasIntegerValue = element.TryGetInt32(out var value);

        if (!hasIntegerValue)
        {
            return null;
        }

        return value;
    }

    public static List<TEnum> ReadEnumList<TEnum>(JsonDocument? document, string property)
        where TEnum : struct, Enum
    {
        if (document is null)
        {
            return [];
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return [];
        }

        var isArray = element.ValueKind == JsonValueKind.Array;

        if (!isArray)
        {
            return [];
        }

        var values = new List<TEnum>();

        foreach (var item in element.EnumerateArray())
        {
            var isNumber = item.ValueKind == JsonValueKind.Number;

            if (isNumber)
            {
                var hasIntegerValue = item.TryGetInt32(out var intValue);

                if (hasIntegerValue)
                {
                    var isDefinedNumber = Enum.IsDefined(typeof(TEnum), intValue);

                    if (isDefinedNumber)
                    {
                        values.Add((TEnum)(object)intValue);
                        continue;
                    }
                }
            }

            var isString = item.ValueKind == JsonValueKind.String;

            if (!isString)
            {
                continue;
            }

            var text = item.GetString();
            var hasEnumValue = Enum.TryParse<TEnum>(text, true, out var enumValue);

            if (hasEnumValue)
            {
                values.Add(enumValue);
            }
        }

        return values;
    }

    public static string? ReadString(JsonDocument? document, string property)
    {
        if (document is null)
        {
            return null;
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return null;
        }

        var isString = element.ValueKind == JsonValueKind.String;

        if (!isString)
        {
            return null;
        }

        return element.GetString();
    }

    public static List<T> ReadList<T>(JsonDocument? document, string property)
    {
        if (document is null)
        {
            return [];
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return [];
        }

        try
        {
            return element.Deserialize<List<T>>(JsonOptions.Default) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static T? ReadObject<T>(JsonDocument? document, string property)
    {
        if (document is null)
        {
            return default;
        }

        var propertyExists = document.RootElement.TryGetProperty(property, out var element);

        if (!propertyExists)
        {
            return default;
        }

        try
        {
            return element.Deserialize<T>(JsonOptions.Default);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
