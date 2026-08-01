using FluentAssertions;

using Netptune.Ai.Execution;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiTitleGeneratorTests
{
    [Theory]
    [InlineData("Sprint planning for the website")]
    [InlineData("  Sprint planning for the website  ")]
    [InlineData("\"Sprint planning for the website\"")]
    [InlineData("“Sprint planning for the website”")]
    [InlineData("Sprint planning for the website.")]
    [InlineData("Sprint  planning\nfor the\twebsite")]
    public void Sanitise_ShouldNormaliseWhatModelsWrapTitlesIn(string raw)
    {
        AiTitleGenerator.Sanitise(raw).Should().Be("Sprint planning for the website");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("...")]
    public void Sanitise_ShouldReturnNull_WhenNothingUsableIsLeft(string? raw)
    {
        AiTitleGenerator.Sanitise(raw).Should().BeNull("a blank title must fall back to the first message");
    }

    [Fact]
    public void Sanitise_ShouldTruncateAnOverlongTitle()
    {
        var raw = new string('a', AiTitleGenerator.MaximumTitleLength + 20);
        var sanitised = AiTitleGenerator.Sanitise(raw);

        sanitised.Should().HaveLength(AiTitleGenerator.MaximumTitleLength + 1);
        sanitised.Should().EndWith("…");
    }
}
