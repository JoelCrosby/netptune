using FluentAssertions;

using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public sealed class EditorDocumentReaderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToMarkdown_ShouldReturnNull_WhenDescriptionIsEmpty(string? value)
    {
        EditorDocumentReader.ToMarkdown(value).Should().BeNull();
    }

    [Fact]
    public void ToMarkdown_ShouldReturnTheValueUntouched_WhenItIsNotAnEditorDocument()
    {
        EditorDocumentReader.ToMarkdown("  Written before the editor existed  ")
            .Should()
            .Be("Written before the editor existed");
    }

    [Fact]
    public void ToMarkdown_ShouldReadHeadingsAndProse()
    {
        var document = """
            {
              "blocks": [
                { "type": "header", "data": { "text": "Installation", "level": 3 } },
                { "type": "paragraph", "data": { "text": "Wrap the <b>inputs</b> in a <i>group</i>." } }
              ]
            }
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("### Installation\n\nWrap the **inputs** in a *group*.");
    }

    [Fact]
    public void ToMarkdown_ShouldReadInlineCodeAndLinks()
    {
        var document = """
            {
              "blocks": [
                { "type": "paragraph", "data": { "text": "Call <code>save()</code> first" } },
                { "type": "paragraph", "data": { "text": "<a href=\"https://netptune.io\">Open it</a>" } }
              ]
            }
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("Call `save()` first\n\n[Open it](https://netptune.io)");
    }

    [Fact]
    public void ToMarkdown_ShouldReadNestedLists()
    {
        var document = """
            {
              "blocks": [
                {
                  "type": "list",
                  "data": {
                    "style": "unordered",
                    "items": [
                      { "content": "Apples", "meta": {}, "items": [{ "content": "Red", "meta": {}, "items": [] }] },
                      { "content": "Pears", "meta": {}, "items": [] }
                    ]
                  }
                }
              ]
            }
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("- Apples\n  - Red\n- Pears");
    }

    [Fact]
    public void ToMarkdown_ShouldReadOrderedListsFromTheirStartingNumber()
    {
        var document = """
            {
              "blocks": [
                {
                  "type": "list",
                  "data": {
                    "style": "ordered",
                    "meta": { "start": 3, "counterType": "numeric" },
                    "items": [
                      { "content": "Open the board", "meta": {}, "items": [] },
                      { "content": "Drag the task", "meta": {}, "items": [] }
                    ]
                  }
                }
              ]
            }
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("3. Open the board\n4. Drag the task");
    }

    [Fact]
    public void ToMarkdown_ShouldReadChecklistsFromBothTools()
    {
        var document = """
            {
              "blocks": [
                {
                  "type": "list",
                  "data": {
                    "style": "checklist",
                    "items": [{ "content": "Convert", "meta": { "checked": true }, "items": [] }]
                  }
                },
                {
                  "type": "checklist",
                  "data": { "items": [{ "text": "Read back", "checked": false }] }
                }
              ]
            }
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("- [x] Convert\n\n- [ ] Read back");
    }

    [Fact]
    public void ToMarkdown_ShouldFenceCode()
    {
        var document = """
            {"blocks":[{"type":"code","data":{"code":"var total = 1;\nvar other = 2;"}}]}
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be("```\nvar total = 1;\nvar other = 2;\n```");
    }

    [Fact]
    public void ToMarkdown_ShouldEscapeMarkdownMarkersThatWereAuthoredAsText()
    {
        var document = """
            {"blocks":[{"type":"paragraph","data":{"text":"a_b_c and 2 * 3"}}]}
            """;

        var markdown = EditorDocumentReader.ToMarkdown(document);

        markdown.Should().Be(@"a\_b\_c and 2 \* 3");
    }

    [Fact]
    public void ToMarkdown_ShouldRoundTripADescriptionWrittenAsMarkdown()
    {
        var markdown = """
            ## Steps

            Some **prose** with `code`.

            - one
            - two
            """;

        var document = EditorDocumentWriter.FromMarkdown(markdown);
        var read = EditorDocumentReader.ToMarkdown(document);

        read.Should().Be("## Steps\n\nSome **prose** with `code`.\n\n- one\n- two");
    }
}
