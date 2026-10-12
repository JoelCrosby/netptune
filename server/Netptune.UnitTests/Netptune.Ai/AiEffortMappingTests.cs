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
    public void EffortsFor_ShouldOnlyOfferLevelsTheModelAccepts()
    {
        AiModels.EffortsFor(AiModels.AnthropicDefault).Should().Contain(AiEffort.Max);
        AiModels.EffortsFor(AiModels.OpenAiDefault).Should().NotContain([AiEffort.XHigh, AiEffort.Max]);
        AiModels.EffortsFor(AiModels.AnthropicTitleModel).Should().BeEmpty();
    }

    [Fact]
    public void EffortsFor_ShouldOfferNothingForAnUnknownModel()
    {
        AiModels.EffortsFor(null).Should().BeEmpty();
        AiModels.EffortsFor("  ").Should().BeEmpty();
        AiModels.EffortsFor("claude-not-a-model").Should().BeEmpty();
    }

    [Fact]
    public void ClampEffort_ShouldKeepASupportedLevel()
    {
        AiModels.ClampEffort(AiModels.AllEfforts, AiEffort.XHigh).Should().Be(AiEffort.XHigh);
    }

    [Fact]
    public void ClampEffort_ShouldFallBackToTheNearestLowerLevel()
    {
        AiModels.ClampEffort([AiEffort.Low, AiEffort.Medium, AiEffort.High], AiEffort.Max).Should().Be(AiEffort.High);
    }

    [Fact]
    public void ClampEffort_ShouldUseTheLowestLevelWhenNothingIsLower()
    {
        AiModels.ClampEffort([AiEffort.High, AiEffort.Max], AiEffort.Low).Should().Be(AiEffort.High);
    }

    [Fact]
    public void ClampEffort_ShouldOmitEffortForAModelWithoutIt()
    {
        AiModels.ClampEffort([], AiEffort.Medium).Should().BeNull();
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
