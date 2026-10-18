using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services.Realtime;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Realtime;

public class BackgroundChangeScopesTests
{
    [Fact]
    public void For_ShouldRefreshTasks_WhenAnAutomationChangesATask()
    {
        var envelope = Envelope(EventKeys.EntityFieldTransitioned, EventOriginType.Automation);

        var scopes = BackgroundChangeScopes.For(envelope);

        scopes.Should().Equal(WorkspaceEventScopes.Task);
    }

    [Fact]
    public void For_ShouldIgnoreChangesMadeByAUser()
    {
        var envelope = Envelope(EventKeys.EntityFieldTransitioned, EventOriginType.User);

        var scopes = BackgroundChangeScopes.For(envelope);

        scopes.Should().BeEmpty();
    }

    [Fact]
    public void For_ShouldIgnoreSourceControlActivity()
    {
        var envelope = Envelope(EventKeys.SourceControlPullRequestMerged, EventOriginType.System);

        var scopes = BackgroundChangeScopes.For(envelope);

        scopes.Should().BeEmpty();
    }

    [Fact]
    public void For_ShouldIncludeChangedMembersButNotContainingScopes()
    {
        var envelope = Envelope(EventKeys.CommentCreated, EventOriginType.Automation) with
        {
            References =
            [
                new CanonicalEventReference(EventReferenceRoles.Member, EventEntityTypes.From(EntityType.Comment), "9"),
                new CanonicalEventReference(EventReferenceRoles.Scope, EventEntityTypes.From(EntityType.Project), "3"),
            ],
        };

        var scopes = BackgroundChangeScopes.For(envelope);

        scopes.Should().BeEquivalentTo([WorkspaceEventScopes.Task, WorkspaceEventScopes.Comment]);
    }

    private static CanonicalEventEnvelope Envelope(string eventKey, EventOriginType originType)
    {
        return new CanonicalEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventRecordId = 1,
            EventKey = eventKey,
            SchemaVersion = 1,
            WorkspaceId = 7,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = "42",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            OriginType = originType,
            RetentionClass = EventRetentionClasses.Audit,
            Payload = JsonDocument.Parse("{}").RootElement,
        };
    }
}
