using FluentAssertions;

using Netptune.Core.Storage;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Storage;

public class ImageUploadTypesTests
{
    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("IMAGE/PNG")]
    [InlineData("image/png; charset=binary")]
    public void IsAllowed_ShouldAccept_WhenContentTypeIsASupportedImage(string contentType)
    {
        ImageUploadTypes.IsAllowed(contentType).Should().BeTrue();
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("image/svg+xml")]
    [InlineData("application/pdf")]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowed_ShouldReject_WhenContentTypeIsNotASupportedImage(string? contentType)
    {
        ImageUploadTypes.IsAllowed(contentType).Should().BeFalse();
    }

    [Fact]
    public void IsInlineSafe_ShouldReject_WhenContentTypeIsSvg()
    {
        ImageUploadTypes.IsInlineSafe("image/svg+xml").Should().BeFalse();
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    public void IsInlineSafe_ShouldAccept_WhenContentTypeCannotCarryScript(string contentType)
    {
        ImageUploadTypes.IsInlineSafe(contentType).Should().BeTrue();
    }

    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("IMAGE/WEBP", ".webp")]
    public void ExtensionFor_ShouldDeriveFromContentType_WhenTypeIsSupported(string contentType, string expected)
    {
        ImageUploadTypes.ExtensionFor(contentType).Should().Be(expected);
    }

    [Fact]
    public void ExtensionFor_ShouldNotReturnACallerChosenExtension_WhenTypeIsUnknown()
    {
        ImageUploadTypes.ExtensionFor("text/html").Should().Be(".bin");
    }
}
