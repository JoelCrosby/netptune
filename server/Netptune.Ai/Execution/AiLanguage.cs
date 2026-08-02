using System.Globalization;

namespace Netptune.Ai.Execution;

public static class AiLanguage
{
    public static string? Describe(string? locale)
    {
        var isMissing = string.IsNullOrWhiteSpace(locale);

        if (isMissing)
        {
            return null;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(locale!.Trim());
            var isInvariant = culture.TwoLetterISOLanguageName == "iv";

            if (isInvariant)
            {
                return null;
            }

            return culture.EnglishName;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
