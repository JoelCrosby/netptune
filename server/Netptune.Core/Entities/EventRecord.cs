using System.Net;
using System.Text.Json;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public sealed record EventRecord : KeyedEntity<long>
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int? WorkspaceId { get; init; }

    public string EventKey { get; init; } = null!;

    public short SchemaVersion { get; init; } = 1;

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public long? SubjectSequence { get; set; }

    public DateTime OccurredAt { get; init; }

    public DateTime RecordedAt { get; init; }

    public string? ActorUserId { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public IPAddress? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string RetentionClass { get; init; } = EventRetentionClasses.Audit;

    public JsonDocument Payload { get; init; } = JsonDocument.Parse("{}");

    public ICollection<EventReference> References { get; init; } = new HashSet<EventReference>();

    public EventOutbox? Outbox { get; init; }

    public bool Equals(EventRecord? other) => other is not null && EventId == other.EventId;

    public override int GetHashCode() => EventId.GetHashCode();
}

public sealed record EventReference
{
    public long EventRecordId { get; init; }

    public string Role { get; init; } = null!;

    public string EntityType { get; init; } = null!;

    public string EntityId { get; init; } = null!;

    public EventRecord EventRecord { get; init; } = null!;

    public bool Equals(EventReference? other)
    {
        return other is not null &&
            EventRecordId == other.EventRecordId &&
            Role == other.Role &&
            EntityType == other.EntityType &&
            EntityId == other.EntityId;
    }

    public override int GetHashCode() => HashCode.Combine(
        EventRecordId,
        Role,
        EntityType,
        EntityId);
}

public sealed record EventStreamHead
{
    public int WorkspaceId { get; init; }

    public string SubjectType { get; init; } = null!;

    public string SubjectId { get; init; } = null!;

    public long CurrentSequence { get; set; }

    public bool Equals(EventStreamHead? other)
    {
        return other is not null &&
            WorkspaceId == other.WorkspaceId &&
            SubjectType == other.SubjectType &&
            SubjectId == other.SubjectId;
    }

    public override int GetHashCode() => HashCode.Combine(WorkspaceId, SubjectType, SubjectId);
}

public sealed record EventOutbox
{
    public long EventRecordId { get; init; }

    public DateTime AvailableAt { get; set; }

    public int AttemptCount { get; set; }

    public Guid? LeaseId { get; set; }

    public DateTime? LeaseExpiresAt { get; set; }

    public string? LastError { get; set; }

    public DateTime? DeadLetteredAt { get; set; }

    public EventRecord EventRecord { get; init; } = null!;

    public bool Equals(EventOutbox? other)
    {
        return other is not null && EventRecordId == other.EventRecordId;
    }

    public override int GetHashCode() => EventRecordId.GetHashCode();
}

public sealed record EventConsumerReceipt
{
    public string ConsumerKey { get; init; } = null!;

    public Guid EventId { get; init; }

    public DateTime ProcessedAt { get; init; }

    public bool Equals(EventConsumerReceipt? other)
    {
        return other is not null &&
            ConsumerKey == other.ConsumerKey &&
            EventId == other.EventId;
    }

    public override int GetHashCode() => HashCode.Combine(ConsumerKey, EventId);
}

public static class EventRetentionClasses
{
    public const string Permanent = "permanent";
    public const string Audit = "audit";
    public const string Operational = "operational";
}

public static class EventReferenceRoles
{
    public const string Scope = "scope";
    public const string Member = "member";
    public const string Parent = "parent";
    public const string Source = "source";
    public const string Target = "target";
}
