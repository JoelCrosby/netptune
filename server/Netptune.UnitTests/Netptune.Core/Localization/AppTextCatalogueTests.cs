using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

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

    [Theory]
    [MemberData(nameof(TranslatedLocales))]
    public void EveryTranslation_ShouldKeepTheEnglishPlaceholders(string locale)
    {
        var english = ReadStrings(CultureInfo.InvariantCulture);
        var translated = ReadStrings(CultureInfo.GetCultureInfo(locale));
        var mismatched = english
            .Where(entry => translated.ContainsKey(entry.Key))
            .Where(entry => !HasSamePlaceholders(entry.Value, translated[entry.Key]))
            .Select(entry => entry.Key)
            .ToList();

        mismatched.Should().BeEmpty("string.Format throws or drops a value when a placeholder goes missing");
    }

    [Theory]
    [MemberData(nameof(TranslatedLocales))]
    public void EveryTranslation_ShouldKeepTheEnglishQuoteMarks(string locale)
    {
        var english = ReadStrings(CultureInfo.InvariantCulture);
        var translated = ReadStrings(CultureInfo.GetCultureInfo(locale));
        var mismatched = english
            .Where(entry => translated.ContainsKey(entry.Key))
            .Where(entry => CountQuotes(entry.Value) > 0)
            .Where(entry => CountQuotes(entry.Value) != CountQuotes(translated[entry.Key]))
            .Select(entry => entry.Key)
            .ToList();

        mismatched.Should().BeEmpty("the client finds the target of a change summary by its “ ” quotes");
    }

    private static bool HasSamePlaceholders(string english, string translated)
    {
        var expected = Placeholders(english);
        var actual = Placeholders(translated);

        return expected.SetEquals(actual);
    }

    private static HashSet<string> Placeholders(string text)
    {
        var matches = Regex.Matches(text, @"\{\d+[^}]*\}");

        return matches.Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
    }

    private static int CountQuotes(string text)
    {
        return text.Count(character => character is '“' or '”');
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
