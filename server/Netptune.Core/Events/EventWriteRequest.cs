namespace Netptune.Core.Events;

public sealed record EventWriteRequest<TPayload> where TPayload : class
{
    public int? WorkspaceId { get; init; }

    public required string EventKey { get; init; }

    public short SchemaVersion { get; init; } = 1;

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public required TPayload Payload { get; init; }

    public IReadOnlyCollection<EventReferenceInput> References { get; init; } = [];

    public string? ActorUserId { get; init; }

    public DateTime? OccurredAt { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public bool Publish { get; init; } = true;
}

public sealed record EventReferenceInput(string Role, string EntityType, string EntityId);

public sealed record EntityCreatedPayload
{
    public string? Name { get; init; }

    public int? StatusId { get; init; }

    public string? StatusCategory { get; init; }

    public int? SprintId { get; init; }

    public string? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }
}

public sealed record FieldTransitionedPayload
{
    public required string Field { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public string? OldCategory { get; init; }

    public string? NewCategory { get; init; }

    public string? OldUnit { get; init; }

    public string? NewUnit { get; init; }

    public decimal? OldNumericValue { get; init; }

    public decimal? NewNumericValue { get; init; }
}

public sealed record ScopeMemberChangedPayload
{
    public required string Change { get; init; }

    public required string MemberType { get; init; }

    public required string MemberId { get; init; }

    public string? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public int? StatusId { get; init; }

    public string? StatusCategory { get; init; }
}

public sealed record ScopeMemberAttributeChangedPayload
{
    public required string MemberType { get; init; }

    public required string MemberId { get; init; }

    public required string Field { get; init; }

    public string? OldUnit { get; init; }

    public string? NewUnit { get; init; }

    public decimal? OldNumericValue { get; init; }

    public decimal? NewNumericValue { get; init; }
}

public sealed record ScopeLifecyclePayload
{
    public required string State { get; init; }

    public DateTime? PlannedStart { get; init; }

    public DateTime? PlannedEnd { get; init; }

    public DateTime? ActualStart { get; init; }

    public DateTime? CompletedAt { get; init; }

    public IReadOnlyCollection<SprintCommitmentMember> Commitment { get; init; } = [];
}

public sealed record SprintCommitmentMember
{
    public int TaskId { get; init; }

    public int StatusId { get; init; }

    public required string StatusCategory { get; init; }

    public string? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }
}
