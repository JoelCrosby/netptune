using FluentAssertions;

using Microsoft.Extensions.Configuration;

using Netptune.Core.Extensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Extensions;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void ReadTimeSpan_returns_configured_value()
    {
        var section = BuildConfiguration("Automation:Schedule:RunInterval", "00:15:00")
            .GetSection("Automation:Schedule");

        var result = section.ReadTimeSpan("RunInterval", TimeSpan.FromHours(1));

        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void ReadTimeSpan_returns_fallback_when_value_is_missing()
    {
        var fallback = TimeSpan.FromHours(1);
        var configuration = BuildConfiguration("Other", "value");

        var result = configuration.ReadTimeSpan("RunInterval", fallback);

        result.Should().Be(fallback);
    }

    [Fact]
    public void ReadTimeSpan_throws_with_section_path_when_value_is_invalid()
    {
        var section = BuildConfiguration("Automation:Schedule:RunInterval", "invalid")
            .GetSection("Automation:Schedule");

        var read = () => section.ReadTimeSpan("RunInterval", TimeSpan.FromHours(1));

        read.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Automation:Schedule:RunInterval must be a valid TimeSpan value*");
    }

    private static IConfiguration BuildConfiguration(string key, string value)
    {
        var values = new Dictionary<string, string?>
        {
            [key] = value,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
