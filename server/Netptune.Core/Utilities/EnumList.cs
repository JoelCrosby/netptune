namespace Netptune.Core.Utilities;

public static class EnumList
{
    public static IReadOnlyList<TEnum> Parse<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => Enum.TryParse<TEnum>(entry, true, out var parsed) ? parsed : (TEnum?)null)
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value)
            .Distinct()
            .ToList();
    }
}
