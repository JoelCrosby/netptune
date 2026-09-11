using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Events;

public sealed class TaskAssigneeTransitionsTests
{
    [Fact]
    public void Split_ShouldWriteOneTransitionPerUser_AddressingOnlyAddedUsers()
    {
        var template = new FieldTransitionedPayload
        {
            Field = "assignees",
            OriginType = EventOriginType.Automation,
            AutomationRuleId = 7,
        };

        var transitions = TaskAssigneeTransitions.Split(template, ["user-1", "user-2"], ["user-3"]);

        transitions.Should().HaveCount(3);

        transitions[0].NewValue.Should().Be("user-1");
        transitions[0].OldValue.Should().BeNull();
        transitions[0].RecipientUserIds.Should().Equal("user-1");

        transitions[1].NewValue.Should().Be("user-2");
        transitions[1].RecipientUserIds.Should().Equal("user-2");

        transitions[2].OldValue.Should().Be("user-3");
        transitions[2].NewValue.Should().BeNull();
        transitions[2].RecipientUserIds.Should().BeNull("an unassigned user is not told they were removed");

        transitions.Should().AllSatisfy(transition =>
        {
            transition.Field.Should().Be(TaskAssigneeTransitions.Field);
            transition.OriginType.Should().Be(EventOriginType.Automation);
            transition.AutomationRuleId.Should().Be(7);
        });
    }

    [Fact]
    public void Split_ShouldWriteNothing_WhenNobodyChanged()
    {
        var template = new FieldTransitionedPayload { Field = TaskAssigneeTransitions.Field };

        var transitions = TaskAssigneeTransitions.Split(template, [], []);

        transitions.Should().BeEmpty();
    }
}
