using FluentAssertions;

using Netptune.Ai.Providers;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiFinishReasonsTests
{
    [Theory]
    [InlineData("refusal", true)]
    [InlineData("Refusal", true)]
    [InlineData("content_filter", true)]
    [InlineData("ContentFilter", true)]
    [InlineData("end_turn", false)]
    [InlineData("Stop", false)]
    [InlineData(null, false)]
    public void IsRefusal_ShouldMatchEitherProvider_InEitherCasing(string? finishReason, bool expected)
    {
        AiFinishReasons.IsRefusal(finishReason).Should().Be(expected);
    }

    [Theory]
    [InlineData("max_tokens", true)]
    [InlineData("MaxTokens", true)]
    [InlineData("length", true)]
    [InlineData("Length", true)]
    [InlineData("end_turn", false)]
    [InlineData(null, false)]
    public void IsOutputLimit_ShouldMatchEitherProvider_InEitherCasing(string? finishReason, bool expected)
    {
        AiFinishReasons.IsOutputLimit(finishReason).Should().Be(expected);
    }
}
