using System.Globalization;

namespace Netptune.Core.Utilities;

public static class FieldValueParser
{
    // Thousands are allowed because spreadsheet imports carry them. Note the consequence under
    // InvariantCulture: "1,5" is one thousand five hundred, not one and a half.
    private const NumberStyles DecimalStyles = NumberStyles.Float | NumberStyles.AllowThousands;

    public static bool TryParseInteger(string value, out int parsed)
    {
        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }

    public static bool TryParseDecimal(string value, out decimal parsed)
    {
        return decimal.TryParse(value.Trim(), DecimalStyles, CultureInfo.InvariantCulture, out parsed);
    }

    public static bool TryParseDate(string value, out DateOnly parsed)
    {
        parsed = default;

        var trimmed = value.Trim();

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return true;
        }

        if (!TryParseTimestamp(trimmed, out var timestamp))
        {
            return false;
        }

        parsed = DateOnly.FromDateTime(timestamp);

        return true;
    }

    public static bool TryParseTimestamp(string value, out DateTime parsed)
    {
        const DateTimeStyles styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

        return DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, styles, out parsed);
    }

    // Enum.TryParse alone would accept any numeric string, including values the enum does not define,
    // so a numeric input is checked against Enum.IsDefined before it is accepted.
    public static bool TryParseEnum(Type enumType, string value, out int parsed)
    {
        parsed = default;

        var trimmed = value.Trim();

        if (TryParseInteger(trimmed, out var numeric))
        {
            if (!Enum.IsDefined(enumType, numeric))
            {
                return false;
            }

            parsed = numeric;

            return true;
        }

        if (!Enum.TryParse(enumType, trimmed, true, out var named))
        {
            return false;
        }

        parsed = (int)named;

        return true;
    }

    public static bool TryParseEnum<TEnum>(string value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;

        if (!TryParseEnum(typeof(TEnum), value, out var numeric))
        {
            return false;
        }

        parsed = (TEnum)Enum.ToObject(typeof(TEnum), numeric);

        return true;
    }
}
