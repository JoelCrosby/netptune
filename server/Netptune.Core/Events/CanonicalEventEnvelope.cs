using System.Text.Json;

namespace Netptune.Core.Events;

public sealed record CanonicalEventEnvelope : IEventMessage
{
    public static string Subject => "netptune.events.v1.recorded";

    public required Guid EventId { get; init; }

    public required string EventKey { get; init; }

    public required short SchemaVersion { get; init; }

    public int? WorkspaceId { get; init; }

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public long? SubjectSequence { get; init; }

    public required DateTime OccurredAt { get; init; }

    public required DateTime RecordedAt { get; init; }

    public string? ActorUserId { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public required string RetentionClass { get; init; }

    public required JsonElement Payload { get; init; }

    public IReadOnlyCollection<CanonicalEventReference> References { get; init; } = [];
}

public sealed record CanonicalEventReference(string Role, string EntityType, string EntityId);
