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
}
