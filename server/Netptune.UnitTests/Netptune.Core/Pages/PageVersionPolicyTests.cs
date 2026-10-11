using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageVersionPolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ShouldCoalesce_ShouldBeTrue_WhenTheSameAuthorSavedWithinTheWindow()
    {
        var latest = Version("user-1", Now.AddMinutes(-9));

        PageVersionPolicy.ShouldCoalesce(latest, "user-1", Now).Should().BeTrue();
    }

    [Fact]
    public void ShouldCoalesce_ShouldBeFalse_WhenTheLatestVersionIsOlderThanTheWindow()
    {
        var latest = Version("user-1", Now.AddMinutes(-10));

        PageVersionPolicy.ShouldCoalesce(latest, "user-1", Now).Should().BeFalse();
    }

    [Fact]
    public void ShouldCoalesce_ShouldBeFalse_WhenSomeoneElseWroteTheLatestVersion()
    {
        var latest = Version("user-2", Now.AddMinutes(-1));

        PageVersionPolicy.ShouldCoalesce(latest, "user-1", Now).Should().BeFalse();
    }

    [Fact]
    public void ShouldCoalesce_ShouldBeFalse_WhenTheLatestVersionIsARestorePoint()
    {
        var latest = Version("user-1", Now.AddMinutes(-1)) with { RestoredFromVersionId = 3 };

        PageVersionPolicy.ShouldCoalesce(latest, "user-1", Now).Should().BeFalse();
    }

    [Fact]
    public void ShouldCoalesce_ShouldBeFalse_WhenTheLatestVersionIsTheFirst()
    {
        var latest = Version("user-1", Now.AddMinutes(-1)) with { Revision = 1 };

        PageVersionPolicy.ShouldCoalesce(latest, "user-1", Now).Should().BeFalse();
    }

    [Fact]
    public void ShouldCoalesce_ShouldBeFalse_WhenThereIsNoVersionYet()
    {
        PageVersionPolicy.ShouldCoalesce(null, "user-1", Now).Should().BeFalse();
    }

    private static PageVersion Version(string author, DateTime createdAt)
    {
        return new PageVersion
        {
            PageId = 1,
            Revision = 5,
            Title = "RFC",
            CreatedByUserId = author,
            CreatedAt = createdAt,
        };
    }
}
