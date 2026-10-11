using FluentAssertions;

using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageSlugTests
{
    [Fact]
    public void Create_ShouldUseTheTitle_FollowedByAnId()
    {
        var slug = PageSlug.Create("Release plan", "Body text");

        slug.Should().MatchRegex("^release-plan-[A-Za-z0-9]{12}$");
    }

    [Fact]
    public void Create_ShouldUseTheFirstLineOfTheBody_WhenThePageIsUntitled()
    {
        var slug = PageSlug.Create(PageTitle.Untitled, "\n## Deploy checklist\n\n- Build the image");

        slug.Should().MatchRegex("^deploy-checklist-[A-Za-z0-9]{12}$");
    }

    [Fact]
    public void Create_ShouldFallBackToUntitled_WhenThereIsNoTitleOrBody()
    {
        var slug = PageSlug.Create(PageTitle.Untitled, null);

        slug.Should().StartWith("untitled-");
    }

    [Fact]
    public void Create_ShouldFallBackToUntitled_WhenTheTitleHasNoUrlSafeCharacters()
    {
        var slug = PageSlug.Create("日本語", null);

        slug.Should().StartWith("untitled-");
    }

    [Fact]
    public void Create_ShouldStayWithinTheSlugLimit_ForALongTitle()
    {
        var slug = PageSlug.Create(new string('a', PageTitle.MaxLength), null);

        slug.Length.Should().BeLessThanOrEqualTo(PageTree.SlugMaxLength);
        PageSlug.IdOf(slug).Should().NotBeNull();
    }

    [Fact]
    public void Follow_ShouldKeepTheId_WhenTheTitleChanges()
    {
        var original = PageSlug.Create(PageTitle.Untitled, null);

        var renamed = PageSlug.Follow(original, "Incident review", null);

        renamed.Should().StartWith("incident-review-");
        PageSlug.IdOf(renamed).Should().Be(PageSlug.IdOf(original));
    }

    [Fact]
    public void Follow_ShouldLeaveTheSlugAlone_WhenItHasNoId()
    {
        var renamed = PageSlug.Follow("legacy", "New title", null);

        renamed.Should().Be("legacy");
    }

    [Theory]
    [InlineData("release-plan-abcdefGHIJ12", "abcdefGHIJ12")]
    [InlineData("abcdefGHIJ12", "abcdefGHIJ12")]
    [InlineData("release-plan", null)]
    [InlineData("release-plan-abcdef_HIJ12", null)]
    public void IdOf_ShouldReadOnlyAWellFormedTrailingId(string slug, string? expected)
    {
        PageSlug.IdOf(slug).Should().Be(expected);
    }
}
