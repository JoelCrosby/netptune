using FluentAssertions;

using Netptune.Core.Extensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("folder/report.pdf", "report.pdf")]
    [InlineData("folder\\report.pdf", "report.pdf")]
    [InlineData("../folder/ résumé\r\n\".pdf", " résumé.pdf")]
    [InlineData("", "")]
    public void SanitizeFileName_ShouldRemovePathsAndUnsafeHeaderCharacters(string input, string expected)
    {
        var result = input.SanitizeFileName();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Release", "release", true)]
    [InlineData("Release", "other", false)]
    [InlineData(null, null, true)]
    [InlineData(null, "release", false)]
    public void EqualsOrdinalIgnoreCase_ShouldCompareNullableValues(string? input, string? value, bool expected)
    {
        var result = input.EqualsOrdinalIgnoreCase(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Urgent request", "URGENT", true)]
    [InlineData("Urgent request", "other", false)]
    [InlineData("Urgent request", "", false)]
    [InlineData(null, "urgent", false)]
    public void ContainsOrdinalIgnoreCase_ShouldCompareNullableValues(string? input, string? value, bool expected)
    {
        var result = input.ContainsOrdinalIgnoreCase(value);

        result.Should().Be(expected);
    }
}
