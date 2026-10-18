using System.Text.Json;

using Netptune.Activity.Realtime;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services.Realtime;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Activity;

public class RealtimeCanonicalEventConsumerTests
{
    private readonly IWorkspaceRefreshQueue RefreshQueue = Substitute.For<IWorkspaceRefreshQueue>();

    [Fact]
    public async Task Handle_ShouldQueueTheChangedScopes_ForAnAutomationChange()
    {
        var consumer = new RealtimeCanonicalEventConsumer(RefreshQueue);

        await consumer.Handle(Envelope(EventOriginType.Automation), TestContext.Current.CancellationToken);

        RefreshQueue.Received(1).Enqueue(
            7,
            Arg.Is<IReadOnlyCollection<string>>(scopes => scopes.Single() == WorkspaceEventScopes.Task));
    }

    [Fact]
    public async Task Handle_ShouldQueueNothing_ForAUserChange()
    {
        var consumer = new RealtimeCanonicalEventConsumer(RefreshQueue);

        await consumer.Handle(Envelope(EventOriginType.User), TestContext.Current.CancellationToken);

        RefreshQueue.DidNotReceiveWithAnyArgs().Enqueue(7, []);
    }

    private static CanonicalEventEnvelope Envelope(EventOriginType originType)
    {
        return new CanonicalEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventRecordId = 1,
            EventKey = EventKeys.EntityFieldTransitioned,
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
