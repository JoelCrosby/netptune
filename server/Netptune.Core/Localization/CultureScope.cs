using System.Globalization;

namespace Netptune.Core.Localization;

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo Previous;

    private CultureScope(CultureInfo culture)
    {
        Previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static CultureScope Use(string? locale)
    {
        var name = AppLocales.Match(locale) ?? AppLocales.Default;
        var culture = CultureInfo.GetCultureInfo(name);

        return new CultureScope(culture);
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = Previous;
    }
}
