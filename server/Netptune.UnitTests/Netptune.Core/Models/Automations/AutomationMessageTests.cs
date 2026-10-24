using FluentAssertions;

using Netptune.Core.Models.Automations;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Models.Automations;

public class AutomationMessageTests
{
    [Theory]
    [InlineData(30, "30 minutes", "30")]
    [InlineData(60, "an hour", "60")]
    [InlineData(90, "1.5 hours", "90")]
    public void DisabledAfterFailures_ShouldCarryTheCountAndWindowInMinutes(
        int windowMinutes,
        string describedWindow,
        string windowArgument)
    {
        var window = TimeSpan.FromMinutes(windowMinutes);

        var message = AutomationMessage.DisabledAfterFailures(5, window);

        message.Code.Should().Be("disabledAfterFailures");
        message.Arguments.Should().Equal("5", windowArgument);
        message.Text.Should().Be($"Disabled automatically after 5 failed runs within {describedWindow}.");
    }

    [Fact]
    public void RelationCycle_ShouldCarryTheRelationTypeName()
    {
        var message = AutomationMessage.RelationCycle("Blocks");

        message.Code.Should().Be("relationCycle");
        message.Arguments.Should().Equal("Blocks");
    }

    [Fact]
    public void Uncoded_ShouldKeepOnlyTheText()
    {
        var message = AutomationMessage.Uncoded("Something broke");

        message.Code.Should().BeNull();
        message.Arguments.Should().BeEmpty();
    }
}
