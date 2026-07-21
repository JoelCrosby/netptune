using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Enums;
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

    [Theory]
    [InlineData(EventKeys.SecurityLoginSucceeded, ActivityType.LoginSuccess)]
    [InlineData(EventKeys.SecurityLoginFailed, ActivityType.LoginFailed)]
    [InlineData(EventKeys.ExportRequested, ActivityType.ExportRequested)]
    [InlineData(EventKeys.WorkspaceRoleChanged, ActivityType.RoleChanged)]
    [InlineData(EventKeys.WorkspaceSettingsChanged, ActivityType.WorkspaceSettingsChanged)]
    public void ActivityTypeFor_ShouldMapAuditEventKeys(string eventKey, ActivityType expected)
    {
        using var payload = JsonDocument.Parse("{}");

        var result = EventKeys.ActivityTypeFor(eventKey, payload.RootElement);

        result.Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldAcceptGlobalSecurityEvent()
    {
        var request = new EventWriteRequest<AuthenticationEventPayload>
        {
            EventKey = EventKeys.SecurityLoginFailed,
            SubjectType = EventEntityTypes.From(EntityType.User),
            SubjectId = "unknown@example.com",
            ResolveActorFromIdentity = false,
            Payload = new AuthenticationEventPayload
            {
                Method = "password",
                Email = "unknown@example.com",
            },
        };

        var action = () => EventDefinitionRegistry.Validate(request);

        action.Should().NotThrow();
    }
}
