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
    public void ToPlainText_ShouldKeepProseAndDropTheMarkdownAroundIt()
    {
        var description = """
            ## Installation

            Wrap the inputs in a role="group" element.
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be("Installation Wrap the inputs in a role=\"group\" element.");
        text.Should().NotContain("#");
    }

    [Fact]
    public void ToPlainText_ShouldStripInlineMarkers()
    {
        var description = "inputs as part of a `div` element with **role** and *scope* and ~~none~~";

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be("inputs as part of a div element with role and scope and none");
    }

    [Fact]
    public void ToPlainText_ShouldKeepIdentifiersThatContainUnderscores()
    {
        RichTextExtractor.ToPlainText("the workspace_id column").Should().Be("the workspace_id column");
    }

    [Fact]
    public void ToPlainText_ShouldReadEveryBlockKind()
    {
        var description = """
            # Getting started

            ```csharp
            controlType = 'example-tel-input';
            ```

            1. Node package
            2. Source from CDN

            - [x] Cookie forwarding works

            > A quoted remark.

            ---

            An [Observer Pattern](https://example.com/observer) link and a ![Login screen](https://cdn.example.com/a.png).
            """;

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().Be(
            "Getting started controlType = 'example-tel-input'; Node package Source from CDN "
            + "Cookie forwarding works A quoted remark. An Observer Pattern link and a Login screen.");
        text.Should().NotContain("cdn.example.com");
        text.Should().NotContain("```");
    }

    [Fact]
    public void ToPlainText_ShouldReadNestedListItems()
    {
        var text = RichTextExtractor.ToPlainText("- Parent\n  - Child");

        text.Should().Be("Parent Child");
    }

    [Fact]
    public void ToPlainText_ShouldReturnNull_WhenTheDescriptionCarriesNoText()
    {
        RichTextExtractor.ToPlainText("---\n\n![](https://cdn.example.com/a.png)").Should().BeNull();
    }

    [Fact]
    public void ToPlainText_ShouldTruncateVeryLongDescriptions()
    {
        var word = new string('a', 9);
        var description = string.Join(' ', Enumerable.Repeat(word, 2_000));

        var text = RichTextExtractor.ToPlainText(description);

        text.Should().NotBeNull();
        text.Length.Should().BeLessThanOrEqualTo(10_000);
        text.Should().EndWith(word);
    }
}
