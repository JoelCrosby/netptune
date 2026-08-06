namespace Netptune.Transfer;

public readonly record struct EntityRef(string Type, string Value)
{
    public const char Separator = ':';

    public override string ToString()
    {
        return $"{Type}{Separator}{Value}";
    }

    public static EntityRef Parse(string value)
    {
        var parsed = TryParse(value, out var result);

        if (!parsed)
        {
            throw new FormatException($"'{value}' is not a valid entity reference.");
        }

        return result;
    }

    public static bool TryParse(string? value, out EntityRef result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separatorIndex = value.IndexOf(Separator);
        var hasType = separatorIndex > 0;
        var hasValue = separatorIndex >= 0 && separatorIndex < value.Length - 1;

        if (!hasType || !hasValue)
        {
            return false;
        }

        result = new EntityRef(value[..separatorIndex], value[(separatorIndex + 1)..]);

        return true;
    }
}
