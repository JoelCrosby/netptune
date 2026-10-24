using FluentAssertions;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiAutomationReferenceTests
{
    [Fact]
    public void Reference_ShouldDescribeEveryTriggerType()
    {
        AiAutomationReference.Triggers.Keys.Should().BeEquivalentTo(Enum.GetValues<AutomationTriggerType>());
    }

    [Fact]
    public void Reference_ShouldDescribeEveryActionType()
    {
        AiAutomationReference.Actions.Keys.Should().BeEquivalentTo(Enum.GetValues<AutomationActionType>());
    }

    [Fact]
    public void Reference_ShouldDescribeEveryConditionField()
    {
        AiAutomationReference.ConditionFields.Keys.Should().BeEquivalentTo(Enum.GetValues<TaskChangeField>());
    }

    [Fact]
    public void Reference_ShouldDescribeEveryConditionOperator()
    {
        AiAutomationReference.ConditionOperators.Keys.Should()
            .BeEquivalentTo(Enum.GetValues<AutomationConditionOperator>());
    }

    [Fact]
    public void Build_ShouldListEveryMessageVariable()
    {
        var variables = AiAutomationReference.Build()["messageVariables"]!.GetValue<string>();

        foreach (var variable in AutomationMessageTemplate.Variables)
        {
            variables.Should().Contain($"{{{{{variable}}}}}");
        }
    }
}
