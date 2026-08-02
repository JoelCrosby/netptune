using FluentAssertions;

using Netptune.Core.Models.Ai;
using Netptune.Core.ViewModels.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiModelPricingTests
{
    [Fact]
    public void RateFor_ShouldCoverEveryCatalogModel()
    {
        foreach (var model in AiModels.Catalog)
        {
            AiModelPricing.RateFor(model.Id).Should().NotBeNull($"{model.Id} needs a published rate");
        }
    }

    [Fact]
    public void RateFor_ShouldIgnoreUnknownModels()
    {
        AiModelPricing.RateFor("not-a-model").Should().BeNull();
        AiModelPricing.RateFor(null).Should().BeNull();
        AiModelPricing.RateFor(" ").Should().BeNull();
    }

    [Fact]
    public void Cost_ShouldPriceEachTokenCategorySeparately()
    {
        var cost = AiModelPricing.Cost("claude-opus-5", 1_000_000, 1_000_000, 1_000_000, 1_000_000);

        cost.Should().Be(5m + 25m + 0.5m + 6.25m);
    }

    [Fact]
    public void Cost_ShouldScaleWithTokenCount()
    {
        var cost = AiModelPricing.Cost("claude-haiku-4-5", 500_000, 100_000, 0, 0);

        cost.Should().Be(0.5m + 0.5m);
    }

    [Fact]
    public void Cost_ShouldBeZeroForAnUnknownModel()
    {
        AiModelPricing.Cost("not-a-model", 1_000_000, 1_000_000, 0, 0).Should().Be(0m);
    }

    [Fact]
    public void WithCost_ShouldPriceUsageWithoutChangingTokenCounts()
    {
        var usage = new AiTokenUsageViewModel
        {
            InputTokens = 200_000,
            OutputTokens = 40_000,
            CacheReadTokens = 1_000_000,
            CacheCreationTokens = 0,
        };

        var priced = usage.WithCost("claude-sonnet-5");

        priced.InputTokens.Should().Be(usage.InputTokens);
        priced.OutputTokens.Should().Be(usage.OutputTokens);
        priced.CacheReadTokens.Should().Be(usage.CacheReadTokens);
        priced.Cost.Should().Be(0.6m + 0.6m + 0.3m);
    }
}
