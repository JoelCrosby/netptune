using System.Collections;
using System.Globalization;
using System.Resources;

using FluentAssertions;

using Netptune.Core.Localization;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Localization;

public class AppTextCatalogueTests
{
    public static TheoryData<string> TranslatedLocales { get; } = new(AppLocales.Supported.Where(IsTranslated));

    [Theory]
    [MemberData(nameof(TranslatedLocales))]
    public void EveryEnglishString_ShouldBeTranslated(string locale)
    {
        var english = ReadStrings(CultureInfo.InvariantCulture);
        var translated = ReadStrings(CultureInfo.GetCultureInfo(locale));
        var missing = english.Keys
            .Where(key => string.IsNullOrWhiteSpace(translated.GetValueOrDefault(key)))
            .ToList();

        english.Should().NotBeEmpty("this guard is worthless if the English catalogue does not load");
        missing.Should().BeEmpty($"every server string needs a {locale} translation");
    }

    [Theory]
    [MemberData(nameof(TranslatedLocales))]
    public void NoTranslation_ShouldHaveAKeyTheEnglishCatalogueLacks(string locale)
    {
        var english = ReadStrings(CultureInfo.InvariantCulture);
        var translated = ReadStrings(CultureInfo.GetCultureInfo(locale));
        var orphaned = translated.Keys.Where(key => !english.ContainsKey(key)).ToList();

        orphaned.Should().BeEmpty("a key renamed in English leaves its old translation behind");
    }

    private static bool IsTranslated(string locale)
    {
        return locale != AppLocales.Default;
    }

    private static Dictionary<string, string> ReadStrings(CultureInfo culture)
    {
        var resources = AppText.ResourceManager.GetResourceSet(culture, true, false);

        if (resources is null)
        {
            return [];
        }

        return resources
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? string.Empty);
    }
}
