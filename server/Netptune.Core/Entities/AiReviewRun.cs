using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiReviewRun : WorkspaceEntity<int>
{
    public EntityType SubjectType { get; init; }

    public int SubjectId { get; init; }

    public AiReviewRunStatus Status { get; init; }

    public Guid CorrelationId { get; init; }

    public required string ReviewedFingerprint { get; init; }

    public required string BaselineFingerprint { get; set; }

    public DateTime? SubjectUpdatedAt { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public string? Summary { get; init; }

    public string? ErrorCode { get; init; }

    public DateTime StartedAt { get; init; }

    [JsonIgnore]
    public ICollection<AiReviewFinding> Findings { get; init; } =
        new HashSet<AiReviewFinding>(ReferenceEqualityComparer.Instance);
}
