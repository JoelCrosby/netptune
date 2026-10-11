using FluentAssertions;

using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageSummaryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(299, 1)]
    [InlineData(300, 2)]
    [InlineData(1_000, 5)]
    public void ReadMinutes_ShouldRoundTheWordsAtTwoHundredAMinute_AndNeverGoBelowOne(long words, int expected)
    {
        PageSummary.ReadMinutes(words).Should().Be(expected);
    }

    [Fact]
    public void Excerpt_ShouldSkipHeadingsAndCode_AndJoinTheFirstTwoLinesOfProse()
    {
        var body = "# Deploy runbook\n\n```\nkubectl apply\n```\n\nRun it on **Fridays** only.\n- Check [[task:NETP-1|the spike]]\nThird line.";

        var excerpt = PageSummary.Excerpt(body, false);

        excerpt.Should().Be("Run it on Fridays only. Check the spike");
    }

    [Fact]
    public void Excerpt_ShouldShortenAtAWord_WithAnEllipsis()
    {
        var body = string.Join(' ', Enumerable.Repeat("word", 60));

        var excerpt = PageSummary.Excerpt(body, false);

        excerpt.Should().EndWith("word…");
        excerpt!.Length.Should().BeLessThanOrEqualTo(161);
    }

    [Fact]
    public void Excerpt_ShouldDropTheLastLine_WhenTheBodyWasCutShort()
    {
        var excerpt = PageSummary.Excerpt("First line.\nHalf a [[task:NETP", true);

        excerpt.Should().Be("First line.");
    }

    [Fact]
    public void Excerpt_ShouldKeepTheCutLine_WhenNothingBeforeItIsProse()
    {
        var paragraph = string.Join(' ', Enumerable.Repeat("word", 600));

        var excerpt = PageSummary.Excerpt($"# Deploy\n{paragraph}", true);

        excerpt.Should().StartWith("word word").And.EndWith("…");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("# Only a heading\n\n{{query:open-bugs}}")]
    public void Excerpt_ShouldReturnNull_WhenThereIsNoProse(string? body)
    {
        PageSummary.Excerpt(body, false).Should().BeNull();
    }
}
