using System.Globalization;

using Netptune.Core.Utilities;

namespace Netptune.Query.Schema;

public static class QueryValueBinder
{
    private const int MaximumRelativeDays = 3650;

    public static bool TryParse(QueryField field, string value, out object? parsed)
    {
        parsed = null;

        if (field.ValueParser is not null)
        {
            return field.ValueParser.TryParse(value, out parsed);
        }

        if (field.EnumType is not null)
        {
            return TryParseEnum(field.EnumType, value, out parsed);
        }

        switch (field.ParameterType)
        {
            case QueryParameterType.Text:
                parsed = value;

                return true;

            case QueryParameterType.Integer:
                return TryParseInteger(value, out parsed);

            case QueryParameterType.Decimal:
                return TryParseDecimal(value, out parsed);

            case QueryParameterType.Date:
            case QueryParameterType.Timestamp:
                return TryParseDate(value, out parsed);

            default:
                return false;
        }
    }

    public static bool TryParseDayCount(string value, out int days)
    {
        var parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out days);
        var isInRange = parsed && days is >= 0 and <= MaximumRelativeDays;

        return isInRange;
    }

    public static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    private static bool TryParseEnum(Type enumType, string value, out object? parsed)
    {
        parsed = null;

        if (!FieldValueParser.TryParseEnum(enumType, value, out var result))
        {
            return false;
        }

        parsed = result;

        return true;
    }

    private static bool TryParseInteger(string value, out object? parsed)
    {
        parsed = null;

        if (!FieldValueParser.TryParseInteger(value, out var result))
        {
            return false;
        }

        parsed = result;

        return true;
    }

    private static bool TryParseDecimal(string value, out object? parsed)
    {
        parsed = null;

        if (!FieldValueParser.TryParseDecimal(value, out var result))
        {
            return false;
        }

        parsed = result;

        return true;
    }

    private static bool TryParseDate(string value, out object? parsed)
    {
        parsed = null;

        if (!FieldValueParser.TryParseDate(value, out var result))
        {
            return false;
        }

        parsed = result;

        return true;
    }
}
