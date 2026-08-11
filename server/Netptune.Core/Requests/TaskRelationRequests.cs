using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public sealed record CreateTaskRelationRequest
{
    [Required]
    public string SourceSystemId { get; init; } = null!;

    [Required]
    public string TargetSystemId { get; init; } = null!;

    public int RelationTypeId { get; init; }
}

public sealed record AddTaskRelationRequest
{
    [Required]
    public string RelatedSystemId { get; init; } = null!;

    public int RelationTypeId { get; init; }

    public bool TaskIsSource { get; init; } = true;
}
