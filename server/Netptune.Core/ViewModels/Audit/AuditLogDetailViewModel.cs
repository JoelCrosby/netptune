namespace Netptune.Core.ViewModels.Audit;

public sealed class AuditLogDetailViewModel : AuditLogViewModel
{
    public Guid EventId { get; init; }

    public required string EventKey { get; init; }

    public short SchemaVersion { get; init; }

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public long? SubjectSequence { get; init; }

    public DateTime RecordedAt { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public required string RetentionClass { get; init; }

    public IReadOnlyList<AuditLogReferenceViewModel> References { get; init; } = [];
}

public sealed record AuditLogReferenceViewModel(string Role, string EntityType, string EntityId);
