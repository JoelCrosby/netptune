using System.Globalization;

using Netptune.Core.Localization;

namespace Netptune.Transfer;

public static class TransferFieldLabels
{
    public static string Header(TransferField field)
    {
        return Header(field, CultureInfo.CurrentUICulture);
    }

    public static IEnumerable<string> HeadersInEveryLocale(TransferField field)
    {
        return AppLocales.Supported
            .Select(CultureInfo.GetCultureInfo)
            .Select(culture => Header(field, culture));
    }

    public static string KeyFor(TransferField field)
    {
        var words = field.Key.Split('.', '_');
        var pascal = words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]);

        return $"TransferField{string.Concat(pascal)}";
    }

    private static string Header(TransferField field, CultureInfo culture)
    {
        var key = KeyFor(field);

        return AppText.ResourceManager.GetString(key, culture) ?? field.Name;
    }
}
