using FluentAssertions;

using Netptune.Ai.Execution;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiLanguageTests
{
    [Theory]
    [InlineData("en-GB", "English")]
    [InlineData("fr", "French")]
    [InlineData("de", "German")]
    [InlineData("es", "Spanish")]
    public void Describe_ShouldNameTheLanguageOfALocale(string locale, string expected)
    {
        AiLanguage.Describe(locale).Should().StartWith(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a locale")]
    public void Describe_ShouldReturnNothing_WhenTheLocaleIsUnusable(string? locale)
    {
        AiLanguage.Describe(locale).Should().BeNull(
            "an unusable locale must leave the prompt alone rather than name a wrong language");
    }
}
