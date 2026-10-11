using FluentAssertions;

using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageTemplatesTests
{
    [Fact]
    public void BuiltIn_ShouldHaveUniqueKeys()
    {
        PageTemplates.BuiltIn.Select(template => template.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ReleaseNotes_ShouldMentionEachCompletedTask_WithANameThatCannotEndTheMention()
    {
        var body = PageTemplates.ReleaseNotes("Sprint 11", [new ReleaseNoteTask("NPT-4", "Fix [login] | logout")]);

        body.Should().Contain("- [[task:NPT-4|Fix [login) / logout]]");
    }

    [Fact]
    public void ReleaseNotesTitle_ShouldFitTheTitleLimit_ForALongSprintName()
    {
        var title = PageTemplates.ReleaseNotesTitle(new string('s', 256));

        title.Length.Should().Be(PageTitle.MaxLength);
    }

    [Fact]
    public void ReleaseNotes_ShouldSaySo_WhenNothingWasCompleted()
    {
        var body = PageTemplates.ReleaseNotes("Sprint 11", []);

        body.Should().Contain("Nothing was completed in this sprint.");
    }
}
