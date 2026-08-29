using System.Text.Json.Nodes;

using FluentAssertions;

using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public sealed class EditorDocumentWriterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromMarkdown_ShouldReturnNull_WhenMarkdownIsEmpty(string? value)
    {
        EditorDocumentWriter.FromMarkdown(value).Should().BeNull();
    }

    [Fact]
    public void FromMarkdown_ShouldReturnTheValueUntouched_WhenItIsAlreadyAnEditorDocument()
    {
        var document = """{"blocks":[{"type":"paragraph","data":{"text":"Already written"}}]}""";

        EditorDocumentWriter.FromMarkdown(document).Should().Be(document);
    }

    [Fact]
    public void FromMarkdown_ShouldWriteHeadings()
    {
        var blocks = Convert("## Steps to reproduce");

        blocks.Should().HaveCount(1);
        blocks[0]!["type"]!.GetValue<string>().Should().Be("header");
        blocks[0]!["data"]!["level"]!.GetValue<int>().Should().Be(2);
        blocks[0]!["data"]!["text"]!.GetValue<string>().Should().Be("Steps to reproduce");
    }

    [Fact]
    public void FromMarkdown_ShouldWriteInlineFormattingAsTheTagsTheEditorUses()
    {
        var blocks = Convert("A **bold** and *italic* line with `code` and a [link](https://netptune.io).");

        var text = blocks[0]!["data"]!["text"]!.GetValue<string>();

        text.Should().Be(
            "A <b>bold</b> and <i>italic</i> line with <code class=\"inline-code\">code</code> "
            + "and a <a href=\"https://netptune.io\">link</a>.");
    }

    [Fact]
    public void FromMarkdown_ShouldEncodeMarkup_SoTheEditorShowsItAsText()
    {
        var blocks = Convert("Wrap the inputs in a <div> & keep the role.");

        var text = blocks[0]!["data"]!["text"]!.GetValue<string>();

        text.Should().Be("Wrap the inputs in a &lt;div&gt; &amp; keep the role.");
    }

    [Fact]
    public void FromMarkdown_ShouldDropALinkTarget_WhenItsSchemeIsNotSafe()
    {
        var blocks = Convert("A [trap](javascript:alert(1)) in the prose.");

        var text = blocks[0]!["data"]!["text"]!.GetValue<string>();

        text.Should().Be("A trap in the prose.");
    }

    [Fact]
    public void FromMarkdown_ShouldWriteNestedUnorderedLists()
    {
        var markdown = """
            - Apples
              - Red
            - Pears
            """;

        var blocks = Convert(markdown);
        var data = blocks[0]!["data"]!;

        blocks[0]!["type"]!.GetValue<string>().Should().Be("list");
        data["style"]!.GetValue<string>().Should().Be("unordered");

        var items = data["items"]!.AsArray();

        items.Should().HaveCount(2);
        items[0]!["content"]!.GetValue<string>().Should().Be("Apples");
        items[0]!["items"]!.AsArray()[0]!["content"]!.GetValue<string>().Should().Be("Red");
        items[1]!["content"]!.GetValue<string>().Should().Be("Pears");
    }

    [Fact]
    public void FromMarkdown_ShouldWriteOrderedListsWithTheirStartingNumber()
    {
        var markdown = """
            2. Open the board
            3. Drag the task
            """;

        var data = Convert(markdown)[0]!["data"]!;

        data["style"]!.GetValue<string>().Should().Be("ordered");
        data["meta"]!["start"]!.GetValue<int>().Should().Be(2);
        data["items"]!.AsArray().Should().HaveCount(2);
    }

    [Fact]
    public void FromMarkdown_ShouldWriteTaskListsAsAChecklist()
    {
        var markdown = """
            - [x] Convert the description
            - [ ] Read it back
            """;

        var data = Convert(markdown)[0]!["data"]!;
        var items = data["items"]!.AsArray();

        data["style"]!.GetValue<string>().Should().Be("checklist");
        items[0]!["meta"]!["checked"]!.GetValue<bool>().Should().BeTrue();
        items[1]!["meta"]!["checked"]!.GetValue<bool>().Should().BeFalse();
        items[1]!["content"]!.GetValue<string>().Should().Be("Read it back");
    }

    [Fact]
    public void FromMarkdown_ShouldStartASecondList_WhenTheKindChanges()
    {
        var markdown = """
            - a bullet

            1. a step

            - [ ] a task
            """;

        var blocks = Convert(markdown);
        var styles = blocks.Select(block => block!["data"]!["style"]!.GetValue<string>());

        blocks.Should().HaveCount(3);
        styles.Should().Equal("unordered", "ordered", "checklist");
    }

    [Fact]
    public void FromMarkdown_ShouldCloseEmphasisThatWrapsAcrossALineBreak()
    {
        var markdown = """
            with **no raw
            markdown** visible
            """;

        var text = Convert(markdown)[0]!["data"]!["text"]!.GetValue<string>();

        text.Should().Be("with <b>no raw<br>markdown</b> visible");
    }

    [Fact]
    public void FromMarkdown_ShouldWriteFencedCodeVerbatim()
    {
        var markdown = """
            ```csharp
            var total = items.Sum(item => item.Value);
            var **kept** = total;
            ```
            """;

        var blocks = Convert(markdown);

        blocks[0]!["type"]!.GetValue<string>().Should().Be("code");
        blocks[0]!["data"]!["code"]!.GetValue<string>().Should().Be(
            "var total = items.Sum(item => item.Value);\nvar **kept** = total;");
    }

    [Fact]
    public void FromMarkdown_ShouldSeparateParagraphsAndKeepTheirLineBreaks()
    {
        var markdown = """
            First line
            second line

            A new paragraph.
            """;

        var blocks = Convert(markdown);

        blocks.Should().HaveCount(2);
        blocks[0]!["data"]!["text"]!.GetValue<string>().Should().Be("First line<br>second line");
        blocks[1]!["data"]!["text"]!.GetValue<string>().Should().Be("A new paragraph.");
    }

    [Fact]
    public void FromMarkdown_ShouldLeaveNoMarkdownSyntaxOnScreen_WhenTheDescriptionUsesEveryBlockKind()
    {
        var markdown = """
            # Title

            Some **prose** here.

            - one
            - two

            ```
            code
            ```

            ---

            > quoted
            """;

        var document = EditorDocumentWriter.FromMarkdown(markdown)!;
        var blocks = Convert(markdown);
        var types = blocks.Select(block => block!["type"]!.GetValue<string>());

        types.Should().Equal("header", "paragraph", "list", "code", "paragraph");
        document.Should().NotContain("#");
        document.Should().NotContain("**");
    }

    private static JsonArray Convert(string markdown)
    {
        var written = EditorDocumentWriter.FromMarkdown(markdown);

        written.Should().NotBeNull();

        var document = JsonNode.Parse(written!)!;

        return document["blocks"]!.AsArray();
    }
}
