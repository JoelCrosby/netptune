using System.Globalization;

namespace Netptune.Core.Localization;

public static class Localized
{
    public static string Format(string template, params object?[] values)
    {
        return string.Format(CultureInfo.CurrentUICulture, template, values);
    }
}
