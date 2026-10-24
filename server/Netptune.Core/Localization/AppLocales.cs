namespace Netptune.Core.Localization;

public static class AppLocales
{
    public const string Default = "en-GB";

    public static IReadOnlyList<string> Supported { get; } = [Default, "fr", "de", "es"];

    public static IReadOnlyDictionary<string, string> NativeNames { get; } = new Dictionary<string, string>
    {
        [Default] = "English (UK)",
        ["fr"] = "Français",
        ["de"] = "Deutsch",
        ["es"] = "Español",
    };

    public static string? Match(string? locale)
    {
        var requested = locale?.Trim();

        if (string.IsNullOrEmpty(requested))
        {
            return null;
        }

        var exact = Supported.FirstOrDefault(name => IsSameName(name, requested));

        if (exact is not null)
        {
            return exact;
        }

        var language = requested.Split('-', '_')[0];

        return Supported.FirstOrDefault(name => SharesLanguage(name, language));
    }

    private static bool IsSameName(string supported, string requested)
    {
        return string.Equals(supported, requested, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SharesLanguage(string supported, string language)
    {
        var supportedLanguage = supported.Split('-')[0];

        return string.Equals(supportedLanguage, language, StringComparison.OrdinalIgnoreCase);
    }
}
