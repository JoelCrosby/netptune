using System.Globalization;

using FluentAssertions;

using Netptune.Core.Localization;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Localization;

public class AppLocalesTests
{
    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("FR", "fr")]
    [InlineData("fr-FR", "fr")]
    [InlineData("de_DE", "de")]
    [InlineData("en", "en-GB")]
    [InlineData("en-US", "en-GB")]
    [InlineData(" es ", "es")]
    public void Match_ShouldResolveToASupportedLocale(string requested, string expected)
    {
        AppLocales.Match(requested).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ja")]
    [InlineData("klingon")]
    public void Match_ShouldReturnNull_ForALocaleTheAppDoesNotShip(string? requested)
    {
        AppLocales.Match(requested).Should().BeNull();
    }

    [Fact]
    public void NativeNames_ShouldNameEverySupportedLocale()
    {
        AppLocales.NativeNames.Keys.Should().BeEquivalentTo(AppLocales.Supported);
    }

    [Fact]
    public void CultureScope_ShouldTranslateInsideTheScope_AndRestoreTheCultureAfter()
    {
        var before = CultureInfo.CurrentUICulture;

        using (CultureScope.Use("fr"))
        {
            AppText.Workspace.Should().Be("Espace de travail");
        }

        CultureInfo.CurrentUICulture.Should().Be(before);
        AppText.Workspace.Should().Be("Workspace");
    }

    [Fact]
    public void CultureScope_ShouldFallBackToEnglish_ForAnUnknownLocale()
    {
        using var scope = CultureScope.Use("ja");

        AppText.Workspace.Should().Be("Workspace");
    }
}
