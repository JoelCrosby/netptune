namespace Netptune.Core.Colors;

public static class NamedColors
{
    public const string Slate = "slate";
    public const string Red = "red";
    public const string Rose = "rose";
    public const string Pink = "pink";
    public const string Fuchsia = "fuchsia";
    public const string Purple = "purple";
    public const string Violet = "violet";
    public const string Indigo = "indigo";
    public const string Blue = "blue";
    public const string Sky = "sky";
    public const string Cyan = "cyan";
    public const string Teal = "teal";
    public const string Emerald = "emerald";
    public const string Green = "green";
    public const string Lime = "lime";
    public const string Yellow = "yellow";
    public const string Amber = "amber";
    public const string Orange = "orange";
    public const string FallbackColor = Slate;

    private static readonly HashSet<string> All =
    [
        Slate,
        Red,
        Rose,
        Pink,
        Fuchsia,
        Purple,
        Violet,
        Indigo,
        Blue,
        Sky,
        Cyan,
        Teal,
        Emerald,
        Green,
        Lime,
        Yellow,
        Amber,
        Orange,
    ];

    public static string? Normalize(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var normalized = color.Trim().ToLowerInvariant();

        return All.Contains(normalized) ? normalized : FallbackColor;
    }
}
