using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageReferenceParserTests
{
    [Fact]
    public void Parse_ShouldReadTaskPageAndUserTokens_WithTheirLine()
    {
        var references = PageReferenceParser.Parse("Tracked in [[task:NETP-701|Spike]] and [[page:42]].\n\nAsk [[user:abc-123]].");

        references.Should().BeEquivalentTo(new[]
        {
            new PageReference(PageLinkTargetType.Task, "netp-701", "Tracked in Spike and 42."),
            new PageReference(PageLinkTargetType.Page, "42", "Tracked in Spike and 42."),
            new PageReference(PageLinkTargetType.User, "abc-123", "Ask abc-123."),
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ShouldSkipTokensInsideCodeFencesAndInlineCode()
    {
        var markdown = "```\n[[task:netp-1]]\n```\nUse `[[task:netp-2]]` to mention.\n[[task:netp-3]]";

        var references = PageReferenceParser.Parse(markdown);

        references.Select(reference => reference.Key).Should().Equal("netp-3");
    }

    [Fact]
    public void Parse_ShouldKeepTheFirstMentionOnly_WhenATaskIsMentionedTwice()
    {
        var references = PageReferenceParser.Parse("- [ ] First [[task:netp-9]]\n- [x] Again [[task:NETP-9]]");

        references.Should().ContainSingle();
        references[0].Snippet.Should().Be("First netp-9");
    }

    [Fact]
    public void Parse_ShouldStripBlockMarkersFromTheSnippet()
    {
        var references = PageReferenceParser.Parse("## Linked [[page:7|Runbook]]");

        references[0].Snippet.Should().Be("Linked Runbook");
    }

    [Fact]
    public void Parse_ShouldTruncateLongSnippets()
    {
        var longLine = new string('a', 300) + " [[page:7]]";

        var references = PageReferenceParser.Parse(longLine);

        references[0].Snippet.Should().HaveLength(PageReferenceParser.SnippetMaxLength).And.EndWith("…");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("No references here. [link](http://example.com) [[not a token]]")]
    public void Parse_ShouldReturnNothing_WhenThereAreNoTokens(string? markdown)
    {
        PageReferenceParser.Parse(markdown).Should().BeEmpty();
    }

    [Fact]
    public void RewritePageIds_ShouldMovePageTokensToTheirNewIds_KeepingLabels()
    {
        var markdown = "See [[page:4|Runbook]] and [[page:5]], not [[task:NET-4]] or [[page:9]].";

        var rewritten = PageReferenceParser.RewritePageIds(markdown, new Dictionary<int, int> { [4] = 40, [5] = 50 });

        rewritten.Should().Be("See [[page:40|Runbook]] and [[page:50]], not [[task:NET-4]] or [[page:9]].");
    }

    [Fact]
    public void RewritePageIds_ShouldLeaveCodeAsWritten()
    {
        var markdown = "`[[page:4]]` then [[page:4]]\n```\n[[page:4]]\n```";

        var rewritten = PageReferenceParser.RewritePageIds(markdown, new Dictionary<int, int> { [4] = 40 });

        rewritten.Should().Be("`[[page:4]]` then [[page:40]]\n```\n[[page:4]]\n```");
    }
}
