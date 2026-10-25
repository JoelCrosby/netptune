using Netptune.Core.Enums;

namespace Netptune.Core.Events;

// Lets requests with different payload types travel in one batch to IEventRecordWriter.AppendRange.
public interface IEventWriteRequest
{
    int? WorkspaceId { get; }

    string EventKey { get; }

    short SchemaVersion { get; }

    string? SubjectType { get; }

    string? SubjectId { get; }

    object Payload { get; }

    Type PayloadType { get; }

    IReadOnlyCollection<EventReferenceInput> References { get; }

    string? ActorUserId { get; }

    bool ResolveActorFromIdentity { get; }

    DateTime? OccurredAt { get; }

    Guid? CorrelationId { get; }

    Guid? CausationEventId { get; }

    EventOriginType? OriginType { get; }

    bool Publish { get; }
}

public sealed record EventWriteRequest<TPayload> : IEventWriteRequest where TPayload : class
{
    public int? WorkspaceId { get; init; }

    public required string EventKey { get; init; }

    public short SchemaVersion { get; init; } = 1;

    public string? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public required TPayload Payload { get; init; }

    public IReadOnlyCollection<EventReferenceInput> References { get; init; } = [];

    public string? ActorUserId { get; init; }

    public bool ResolveActorFromIdentity { get; init; } = true;

    public DateTime? OccurredAt { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationEventId { get; init; }

    public EventOriginType? OriginType { get; init; }

    public bool Publish { get; init; } = true;

    object IEventWriteRequest.Payload => Payload;

    Type IEventWriteRequest.PayloadType => typeof(TPayload);
}

public sealed record EventReferenceInput
{
    public required string Role { get; init; }

    public required string EntityType { get; init; }

    public required string EntityId { get; init; }
}

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

    public EventOriginType OriginType { get; init; }

    public int? AutomationRuleId { get; init; }

    public int? AutomationRunId { get; init; }

    public int ChainDepth { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public string? OldCategory { get; init; }

    public string? NewCategory { get; init; }

    public string? OldUnit { get; init; }

    public string? NewUnit { get; init; }

    public decimal? OldNumericValue { get; init; }

    public decimal? NewNumericValue { get; init; }

    public IReadOnlyCollection<string>? RecipientUserIds { get; init; }
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

public sealed record AuthenticationEventPayload
{
    public required string Method { get; init; }

    public string? Email { get; init; }
}

public sealed record ExportRequestedPayload
{
    public required string ExportType { get; init; }

    public string? Scope { get; init; }
}

public sealed record ImportCompletedPayload
{
    public required string RecordType { get; init; }

    public required string SourceKind { get; init; }

    public int Created { get; init; }

    public int Updated { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }

    public string? VendorProfile { get; init; }

    public string? Error { get; init; }
}

public sealed record ExportCompletedPayload
{
    public required string Format { get; init; }

    public string? FileName { get; init; }

    public long? RowCount { get; init; }

    public long? SizeBytes { get; init; }

    public string? Error { get; init; }
}

public sealed record WorkspaceRoleChangedPayload
{
    public required string TargetUserId { get; init; }

    public required string OldRole { get; init; }

    public required string NewRole { get; init; }
}

public sealed record WorkspaceSettingsChangedPayload
{
    public IReadOnlyCollection<string> Fields { get; init; } = [];
}

public sealed record CommentEventPayload
{
    public int CommentId { get; init; }

    public IReadOnlyCollection<string> RecipientUserIds { get; init; } = [];
}

public sealed record EntityDeletedPayload
{
    public required string Name { get; init; }
}

public sealed record FlagRaisedPayload
{
    public required string FlagName { get; init; }

    public int? AutomationRuleId { get; init; }
}

public sealed record TaskRelationChangedPayload
{
    public required string Change { get; init; }

    public int RelationTypeId { get; init; }

    public int SourceTaskId { get; init; }

    public int TargetTaskId { get; init; }
}

public sealed record FlagResolutionPayload
{
    public int FlagId { get; init; }

    public required string FlagName { get; init; }

    public required string Resolution { get; init; }

    public int? AutomationRuleId { get; init; }
}

public sealed record AssistantConversationDeletedPayload
{
    public Guid ConversationId { get; init; }

    public required string Title { get; init; }

    public required string OwnerUserId { get; init; }

    public string? OwnerDisplayName { get; init; }
}

public sealed record SourceControlConnectionEventPayload
{
    public int ConnectionId { get; init; }

    public required string Provider { get; init; }

    public required string AccountLogin { get; init; }

    public required string ExternalAccountId { get; init; }
}

public sealed record SourceControlActivityPayload
{
    public required string Provider { get; init; }

    public required string RepoFullName { get; init; }

    // The pull request number, the branch for pushed commits, or "#number" / a short SHA for checks.
    public required string Reference { get; init; }

    public required string Title { get; init; }

    public required string WebUrl { get; init; }

    public int? CommitCount { get; init; }

    public int? ChecksTotal { get; init; }

    public int? ChecksFailed { get; init; }

    public string? ActorLogin { get; init; }

    public string? ActorAvatarUrl { get; init; }
}

public sealed record AssistantReviewCompletedPayload
{
    public int RunId { get; init; }

    public required string Outcome { get; init; }

    public string? ErrorCode { get; init; }

    public int FindingCount { get; init; }

    public required string Model { get; init; }

    public required string ReviewedFingerprint { get; init; }

    public DateTime? SubjectUpdatedAt { get; init; }
}

public sealed record AssistantReviewFindingDecidedPayload
{
    public int RunId { get; init; }

    public long FindingId { get; init; }

    public required string Category { get; init; }

    public required string Title { get; init; }

    public string? Reason { get; init; }

    public bool Edited { get; init; }

    public bool Reverted { get; init; }

    public List<AssistantReviewAppliedEntity> Changes { get; init; } = [];
}

public sealed record AssistantReviewAppliedEntity
{
    public required string Kind { get; init; }

    public required string EntityType { get; init; }

    public int EntityId { get; init; }
}
