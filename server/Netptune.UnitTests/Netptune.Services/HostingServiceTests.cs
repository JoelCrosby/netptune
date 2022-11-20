using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Netptune.Core.Models.Hosting;
using Netptune.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class HostingServiceTests
{
    private static readonly Fixture Fixture = new();

    private readonly HostingService Service;
    private readonly IOptionsMonitor<HostingOptions> Options = Substitute.For<IOptionsMonitor<HostingOptions>>();

    private readonly HostingOptions HostingOptions = Fixture.Create<HostingOptions>();

    public HostingServiceTests()
    {
        Options.CurrentValue.Returns(HostingOptions);
        Service = new(Options);
    }

    [Fact]
    public void HostingService_ShouldProvideCorrectOptions()
    {
        Service.ClientOrigin.Should().Be(HostingOptions.ClientOrigin);
        Service.ContentRootPath.Should().Be(HostingOptions.ContentRootPath);
    }
}
