using System.Net;

using FluentAssertions;

using Netptune.Ai.Web;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class WebEgressGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.53.1.9")]
    [InlineData("10.0.0.7")]
    [InlineData("172.16.4.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:127.0.0.1")]
    public void CheckAddress_ShouldBlock_PrivateAndLocalAddresses(string value)
    {
        var verdict = WebEgressGuard.CheckAddress(IPAddress.Parse(value));

        verdict.IsAllowed.Should().BeFalse($"{value} is not reachable from the public internet");
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946")]
    public void CheckAddress_ShouldAllow_PublicAddresses(string value)
    {
        var verdict = WebEgressGuard.CheckAddress(IPAddress.Parse(value));

        verdict.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/pub")]
    [InlineData("gopher://example.com")]
    public void CheckUrl_ShouldBlock_NonHttpSchemes(string value)
    {
        var verdict = WebEgressGuard.CheckUrl(new Uri(value));

        verdict.IsAllowed.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://localhost:7400/api/tasks")]
    [InlineData("http://api.localhost/")]
    [InlineData("http://postgres.internal/")]
    [InlineData("http://printer.local/")]
    public void CheckUrl_ShouldBlock_LocalHostNames(string value)
    {
        var verdict = WebEgressGuard.CheckUrl(new Uri(value));

        verdict.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void CheckUrl_ShouldBlock_UrlsCarryingCredentials()
    {
        var verdict = WebEgressGuard.CheckUrl(new Uri("https://user:secret@example.com/"));

        verdict.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void CheckUrl_ShouldBlock_LiteralPrivateAddresses()
    {
        var verdict = WebEgressGuard.CheckUrl(new Uri("http://169.254.169.254/latest/meta-data/"));

        verdict.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void CheckUrl_ShouldAllow_PublicHttps()
    {
        var verdict = WebEgressGuard.CheckUrl(new Uri("https://example.com/docs"));

        verdict.IsAllowed.Should().BeTrue();
    }
}
