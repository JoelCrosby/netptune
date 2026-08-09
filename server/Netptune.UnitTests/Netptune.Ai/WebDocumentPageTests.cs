using FluentAssertions;

using Netptune.Ai.Web;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WebDocumentPageTests
{
    [Fact]
    public void ShouldReportTheWholeDocument_WhenItFitsInOnePage()
    {
        var page = WebDocumentPage.Read("A short page.", 0, 6000);

        page.Text.Should().Be("A short page.");
        page.HasMore.Should().BeFalse();
        page.NextOffset.Should().Be(13);
    }

    [Fact]
    public void ShouldWalkTheDocument_WithoutGapsOrOverlap()
    {
        var content = string.Join("\n", Enumerable.Range(0, 400).Select(index => $"line {index}"));
        var visited = string.Empty;
        var offset = 0;

        while (true)
        {
            var page = WebDocumentPage.Read(content, offset, 250);

            visited += page.Text;
            offset = page.NextOffset;

            if (!page.HasMore)
            {
                break;
            }
        }

        visited.Should().Be(content);
    }

    [Fact]
    public void ShouldBreakOnALineBoundary_RatherThanMidWord()
    {
        var content = string.Join("\n", Enumerable.Repeat("the quick brown fox jumps", 40));

        var page = WebDocumentPage.Read(content, 0, 100);

        page.Text.Should().EndWith("\n");
        page.HasMore.Should().BeTrue();
    }

    [Fact]
    public void ShouldClampAnOffsetBeyondTheDocument()
    {
        var page = WebDocumentPage.Read("Short", 5000, 100);

        page.Text.Should().BeEmpty();
        page.HasMore.Should().BeFalse();
    }
}
