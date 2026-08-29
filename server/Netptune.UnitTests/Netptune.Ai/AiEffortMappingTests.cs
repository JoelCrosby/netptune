using System.Text.Json;

using Anthropic.Models.Messages;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiEffortMappingTests
{
    [Fact]
    public void SupportsEffort_ShouldRejectAModelThatTheApiWouldError()
    {
        AiModels.SupportsEffort(AiModels.AnthropicDefault).Should().BeTrue();
        AiModels.SupportsEffort("claude-sonnet-5").Should().BeTrue();
        AiModels.SupportsEffort(AiModels.AnthropicTitleModel).Should().BeFalse();
    }

    [Fact]
    public void SupportsEffort_ShouldRejectAnUnknownModel()
    {
        AiModels.SupportsEffort(null).Should().BeFalse();
        AiModels.SupportsEffort("  ").Should().BeFalse();
        AiModels.SupportsEffort("claude-not-a-model").Should().BeFalse();
    }

    [Fact]
    public void EveryEffortLevel_ShouldMapToAnAnthropicLevel()
    {
        foreach (var effort in Enum.GetValues<AiEffort>())
        {
            var config = new OutputConfig { Effort = CreateEffort(effort) };

            config.Effort.Should().NotBeNull();
        }
    }

    // The Haiku title model does not accept output_config, so the mapping leaves it null. That is only
    // safe while the SDK omits a null property rather than writing an explicit null the API would reject.
    [Fact]
    public void ANullOutputConfig_ShouldNotReachTheWire()
    {
        var parameters = new MessageCreateParams
        {
            Model = AiModels.AnthropicTitleModel,
            MaxTokens = 16,
            Messages = [new MessageParam { Role = Role.User, Content = "hello" }],
            OutputConfig = null,
        };

        var json = JsonSerializer.Serialize(parameters);

        json.Should().NotContain("output_config");
    }

    private static Effort CreateEffort(AiEffort effort)
    {
        return effort switch
        {
            AiEffort.Low => Effort.Low,
            AiEffort.Medium => Effort.Medium,
            AiEffort.XHigh => Effort.Xhigh,
            AiEffort.Max => Effort.Max,
            _ => Effort.High,
        };
    }
}
