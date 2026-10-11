using FluentAssertions;

using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageSearchTextTests
{
    [Fact]
    public void ToPlainText_ShouldIndexMentionsAsTheirLabels()
    {
        var text = PageSearchText.ToPlainText("Tracked in [[task:NETP-701|Offline sync spike]] and [[page:42|Roadmap]].");

        text.Should().Be("Tracked in Offline sync spike and Roadmap.");
    }

    [Fact]
    public void ToPlainText_ShouldIndexProjectSprintAndBoardMentionsAsTheirLabels()
    {
        var text = PageSearchText.ToPlainText("Sprint: [[sprint:16|End-to-End Observability]] (project [[project:8|Monitoring]]) on [[board:3|Ops]]");

        text.Should().Be("Sprint: End-to-End Observability (project Monitoring) on Ops");
    }

    [Fact]
    public void ToPlainText_ShouldFallBackToTheKey_WhenAMentionHasNoLabel()
    {
        var text = PageSearchText.ToPlainText("See [[task:NETP-7]].");

        text.Should().Be("See NETP-7.");
    }

    [Fact]
    public void ToPlainText_ShouldDropQueryEmbedsAndCalloutMarkers()
    {
        var markdown = "# Status\n\n{{query:open-bugs-AbC123}}\n\n> [!WARNING]\n> Freeze starts Friday.";

        var text = PageSearchText.ToPlainText(markdown);

        text.Should().Be("Status Freeze starts Friday.");
    }

    [Fact]
    public void ToPlainText_ShouldKeepAnEmbedWrittenInsideProse()
    {
        var text = PageSearchText.ToPlainText("Write {{query:slug}} on its own line to embed.");

        text.Should().Be("Write {{query:slug}} on its own line to embed.");
    }

    [Fact]
    public void ToPlainText_ShouldLeaveCodeAsWritten()
    {
        var markdown = "```\n{{query:open-bugs}}\n[[task:NETP-1|Spike]]\n```\nUse `[[page:4]]` to link.";

        var text = PageSearchText.ToPlainText(markdown);

        text.Should().Be("{{query:open-bugs}} [[task:NETP-1|Spike]] Use [[page:4]] to link.");
    }

    [Fact]
    public void ToPlainText_ShouldDropTheEscape_WhenAnEmbedIsWrittenAsProse()
    {
        var text = PageSearchText.ToPlainText("\\{{query:open-bugs}}");

        text.Should().Be("{{query:open-bugs}}");
    }

    [Fact]
    public void ToPlainText_ShouldDropAUserMention_WhenItHasNoLabel()
    {
        var text = PageSearchText.ToPlainText("Ask [[user:3f2a-91]] or [[user:7b|Sam]].");

        text.Should().Be("Ask or Sam.");
    }

    [Fact]
    public void ToPlainText_ShouldKeepAnUppercaseQueryLine_WhichTheEditorShowsAsText()
    {
        var text = PageSearchText.ToPlainText("{{QUERY:open-bugs}}");

        text.Should().Be("{{QUERY:open-bugs}}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_ShouldReturnNull_WhenThereIsNoText(string? body)
    {
        PageSearchText.ToPlainText(body).Should().BeNull();
    }
}
