using FluentAssertions;

using Netptune.Core.Models.ProjectTasks;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.ProjectTasks;

public class ProjectTaskDescriptionTests
{
    [Theory]
    [InlineData("first\r\nsecond", "first\nsecond")]
    [InlineData("first\rsecond", "first\nsecond")]
    [InlineData("text\n\n  \t\n", "text")]
    [InlineData("text\r\n\r\n", "text")]
    [InlineData("  \r\n\t ", "")]
    public void Normalize_ShouldUnifyLineEndingsAndTrimTheEnd(string description, string expected)
    {
        ProjectTaskDescription.Normalize(description).Should().Be(expected);
    }

    [Theory]
    [InlineData("hard  \nbreak")]
    [InlineData("paused after a space \n\nnext paragraph")]
    [InlineData("- [ ] \n- [ ] next item")]
    [InlineData("```\ncode with trailing spaces  \n\n```")]
    public void Normalize_ShouldLeaveEditorOutputUnchanged(string description)
    {
        ProjectTaskDescription.Normalize(description).Should().Be(description);
    }

    [Theory]
    [InlineData("Returns Array<string> from <div class=\"x\">")]
    [InlineData("<script>alert(1)</script>")]
    public void Normalize_ShouldKeepRawHtmlVerbatim(string description)
    {
        ProjectTaskDescription.Normalize(description).Should().Be(description);
    }

    [Fact]
    public void Normalize_ShouldKeepLeadingWhitespace()
    {
        ProjectTaskDescription.Normalize("    indented code").Should().Be("    indented code");
    }

    [Fact]
    public void Normalize_ShouldKeepNull_SoAnUpdateCanLeaveTheDescriptionAlone()
    {
        ProjectTaskDescription.Normalize(null).Should().BeNull();
    }

    [Fact]
    public void IsValid_ShouldAcceptUpToTheLimit()
    {
        ProjectTaskDescription.IsValid(null).Should().BeTrue();
        ProjectTaskDescription.IsValid(new string('a', ProjectTaskDescription.MaxLength)).Should().BeTrue();
        ProjectTaskDescription.IsValid(new string('a', ProjectTaskDescription.MaxLength + 1)).Should().BeFalse();
    }
}
