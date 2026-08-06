using Netptune.Transfer.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Netptune.Transfer.Import;

public static partial class ImportTransforms
{
    public static string? Apply(string? value, IReadOnlyList<ImportTransform> transforms)
    {
        var result = value;

        foreach (var transform in transforms)
        {
            result = ApplyOne(result, transform);
        }

        return result;
    }

    public static IReadOnlyList<string> Split(string? value, IReadOnlyList<ImportTransform> transforms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var splitOn = transforms.FirstOrDefault(transform => transform.Kind == ImportTransformKind.SplitOn);
        var separator = splitOn?.Argument;

        if (string.IsNullOrEmpty(separator))
        {
            separator = "|";
        }

        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string? ApplyOne(string? value, ImportTransform transform)
    {
        return transform.Kind switch
        {
            ImportTransformKind.Trim => value?.Trim(),
            ImportTransformKind.Lowercase => value?.ToLowerInvariant(),
            ImportTransformKind.Uppercase => value?.ToUpperInvariant(),
            ImportTransformKind.StripHtml => StripHtml(value),
            ImportTransformKind.Coalesce => string.IsNullOrWhiteSpace(value) ? transform.Argument : value,
            ImportTransformKind.Truncate => Truncate(value, transform.Argument),
            ImportTransformKind.Prefix => value is null ? null : $"{transform.Argument}{value}",
            ImportTransformKind.Suffix => value is null ? null : $"{value}{transform.Argument}",
            _ => value,
        };
    }

    private static string? StripHtml(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return HtmlTag().Replace(value, string.Empty);
    }

    private static string? Truncate(string? value, string? argument)
    {
        var parsed = int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length);

        if (value is null || !parsed || length <= 0 || value.Length <= length)
        {
            return value;
        }

        return value[..length];
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();
}
