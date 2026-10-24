using System.Globalization;

namespace Netptune.Core.Localization;

public static class LocalizedEnum
{
    public static string Label<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var key = KeyFor(value);

        return AppText.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? value.ToString();
    }

    public static string KeyFor<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return $"{typeof(TEnum).Name}{value}";
    }
}
