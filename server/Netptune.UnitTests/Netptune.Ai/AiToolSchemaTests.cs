using FluentAssertions;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiToolSchemaTests
{
    [Theory]
    [InlineData("High", true)]
    [InlineData("high", true)]
    [InlineData(" Critical ", true)]
    [InlineData("99", false)]
    [InlineData("1", false)]
    [InlineData("-1", false)]
    [InlineData("High, Low", false)]
    [InlineData("Urgent", false)]
    [InlineData("", false)]
    public void TryParseEnum_ShouldAcceptOnlyASingleDefinedName(string raw, bool expected)
    {
        AiToolSchema.TryParseEnum<TaskPriority>(raw, out _).Should().Be(expected);
    }

    [Fact]
    public void Skip_ShouldNotWrapRound_ForAHugePageNumber()
    {
        AiToolSchema.Skip(int.MaxValue, 100).Should().Be(int.MaxValue);
        AiToolSchema.Skip(0, 100).Should().Be(0);
        AiToolSchema.Skip(3, 10).Should().Be(20);
    }

    [Fact]
    public void HasMore_ShouldNotWrapRound_ForAHugePageNumber()
    {
        AiToolSchema.HasMore(int.MaxValue, 100, 50).Should().BeFalse();
        AiToolSchema.HasMore(2, 10, 25).Should().BeTrue();
    }
}
