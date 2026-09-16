using FluentAssertions;

using Netptune.Core.Storage;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Storage;

public class StorageKeysTests
{
    [Fact]
    public void TryResolveProfilePictureKey_ShouldReturnKey_WhenUrlPointsAtOurProfilePicturePath()
    {
        const string url = "https://bucket.s3.eu-west-2.amazonaws.com/user/profile/abc-123.png";

        var key = StorageKeys.TryResolveProfilePictureKey(url);

        key.Should().Be("user/profile/abc-123.png");
    }

    [Fact]
    public void TryResolveProfilePictureKey_ShouldReturnNull_WhenUrlIsAnExternalProviderAvatar()
    {
        const string url = "https://avatars.githubusercontent.com/u/4275668?v=4";

        var key = StorageKeys.TryResolveProfilePictureKey(url);

        key.Should().BeNull();
    }

    [Fact]
    public void TryResolveProfilePictureKey_ShouldReturnNull_WhenUrlPointsAtAnotherStoragePath()
    {
        const string url = "https://bucket.s3.eu-west-2.amazonaws.com/workspace/1/files/9/abc.png";

        var key = StorageKeys.TryResolveProfilePictureKey(url);

        key.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    public void TryResolveProfilePictureKey_ShouldReturnNull_WhenUrlIsMissingOrUnparseable(string? url)
    {
        var key = StorageKeys.TryResolveProfilePictureKey(url);

        key.Should().BeNull();
    }
}
