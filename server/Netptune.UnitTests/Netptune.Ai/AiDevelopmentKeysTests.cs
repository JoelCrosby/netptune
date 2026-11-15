using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiDevelopmentKeysTests
{
    [Fact]
    public void Options_ShouldFailValidation_WhenADevelopmentKeyIsSetOutsideDevelopment()
    {
        using var provider = CreateProvider("sk-dev", isDevelopment: false);

        var read = () => provider.GetRequiredService<IOptions<AiOptions>>().Value;

        read.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Ai:DevelopmentKeys");
    }

    [Fact]
    public void Options_ShouldBind_WhenADevelopmentKeyIsSetInDevelopment()
    {
        using var provider = CreateProvider("sk-dev", isDevelopment: true);

        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        options.DevelopmentKeys.Anthropic.Should().Be("sk-dev");
    }

    [Fact]
    public void Options_ShouldBind_WhenNoDevelopmentKeyIsSetOutsideDevelopment()
    {
        using var provider = CreateProvider(null, isDevelopment: false);

        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        options.DevelopmentKeys.HasAny().Should().BeFalse();
    }

    private static ServiceProvider CreateProvider(string? anthropicKey, bool isDevelopment)
    {
        var settings = new Dictionary<string, string?> { ["Ai:DevelopmentKeys:Anthropic"] = anthropicKey };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        services.AddNetptuneAi(configuration, isDevelopment);

        return services.BuildServiceProvider();
    }
}
