using FluentAssertions;

using Netptune.Core.Events;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Events;

public sealed class EventDefinitionRegistryTests
{
    [Fact]
    public void Validate_ShouldAcceptRegisteredPayload()
    {
        var request = new EventWriteRequest<FieldTransitionedPayload>
        {
            WorkspaceId = 1,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = "task",
            SubjectId = "42",
            Payload = new FieldTransitionedPayload { Field = "status" },
        };

        var action = () => EventDefinitionRegistry.Validate(request);

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectWrongPayloadType()
    {
        var request = new EventWriteRequest<EntityCreatedPayload>
        {
            WorkspaceId = 1,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = "task",
            SubjectId = "42",
            Payload = new EntityCreatedPayload(),
        };

        var action = () => EventDefinitionRegistry.Validate(request);

        action.Should().Throw<InvalidOperationException>().WithMessage("*requires payload*");
    }
}
