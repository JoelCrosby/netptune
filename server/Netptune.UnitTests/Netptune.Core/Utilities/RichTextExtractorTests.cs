using FluentAssertions;

using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public sealed class RichTextExtractorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_ShouldReturnNull_WhenDescriptionIsEmpty(string? value)
    {
        RichTextExtractor.ToPlainText(value).Should().BeNull();
    }

    [Fact]
    public void ToPlainText_ShouldCollapseWhitespace_WhenDescriptionIsPlainText()
    {
        var text = RichTextExtractor.ToPlainText("  Fix the  login\n\nredirect  ");

        text.Should().Be("Fix the login redirect");
    }

    [Fact]
    public void ToPlainText_ShouldKeepProseAndDropDocumentScaffolding_WhenDescriptionIsAnEditorDocument()
    {
        var description = """
            {
              "time": 1755000000000,
              "blocks": [
                { "id": "s8yaTRa7jF", "type": "header", "data": { "text": "Installation", "level": 2 } },
                { "id": "IQClfDVSE1", "type": "paragraph", "data": { "text": "Wrap the inputs in a role=\"group\" element." } }
              ],
              "version": "2.31.6"
            }
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be("Installation Wrap the inputs in a role=\"group\" element.");
        text.Should().NotContain("paragraph");
        text.Should().NotContain("s8yaTRa7jF");
        text.Should().NotContain("2.31.6");
    }

    [Fact]
    public void ToPlainText_ShouldStripInlineMarkupAndEntities()
    {
        var description = """
            {"blocks":[{"type":"paragraph","data":{"text":"inputs as part of a&nbsp;<code>div</code>&nbsp;element with <b>role</b>"}}]}
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be("inputs as part of a div element with role");
    }

    [Fact]
    public void ToPlainText_ShouldReadEveryBlockType()
    {
        var description = """
            {"blocks":[
              {"type":"code","data":{"code":"controlType = 'example-tel-input';\n"}},
              {"type":"list","data":{"style":"ordered","items":["Node package","Source from CDN"]}},
              {"type":"checklist","data":{"items":[{"text":"Cookie forwarding works","checked":true}]}},
              {"type":"link","data":{"link":"https://example.com/observer","meta":{"title":"Observer Pattern","description":"Learn the pattern."}}},
              {"type":"image","data":{"caption":"Login screen","file":{"url":"https://cdn.example.com/kwANG5w.png","name":"image.png"}}},
              {"type":"attaches","data":{"title":"spec.pdf","file":{"name":"spec.pdf","url":"https://cdn.example.com/spec.pdf"}}}
            ]}
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be(
            "controlType = 'example-tel-input'; Node package Source from CDN Cookie forwarding works "
            + "https://example.com/observer Observer Pattern Learn the pattern. Login screen spec.pdf");
        text.Should().NotContain("cdn.example.com/kwANG5w.png");
        text.Should().NotContain("image.png");
    }

    [Fact]
    public void ToPlainText_ShouldReadNestedListItems()
    {
        var description = """
            {"blocks":[{"type":"list","data":{"style":"unordered","items":[
              {"content":"Parent","items":[{"content":"Child","items":[]}]}
            ]}}]}
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be("Parent Child");
    }

    [Fact]
    public void ToPlainText_ShouldFallBackToRawText_WhenJsonIsNotAnEditorDocument()
    {
        RichTextExtractor.ToPlainText("{ not really json").Should().Be("{ not really json");
        RichTextExtractor.ToPlainText("""{"note":"a stray object"}""").Should().Be("{\"note\":\"a stray object\"}");
    }

    [Fact]
    public void ToPlainText_ShouldReturnNull_WhenTheDocumentCarriesNoText()
    {
        var description = """
            {"blocks":[{"type":"image","data":{"caption":"","file":{"url":"https://cdn.example.com/a.png"}}}],"version":"2.31.6"}
            """;

        RichTextExtractor.ToPlainText(description).Should().BeNull();
    }

    [Fact]
    public void ToPlainText_ShouldTruncateVeryLongDescriptions()
    {
        var word = new string('a', 9);
        var paragraph = string.Join(' ', Enumerable.Repeat(word, 2_000));
        var description = "{\"blocks\":[{\"type\":\"paragraph\",\"data\":{\"text\":\"" + paragraph + "\"}}]}";

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().NotBeNull();
        text.Length.Should().BeLessThanOrEqualTo(10_000);
        text.Should().EndWith(word);
    }
}
