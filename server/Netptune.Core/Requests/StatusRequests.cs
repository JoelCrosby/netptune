using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public sealed record StatusFilter
{
    public EntityType EntityType { get; init; } = EntityType.Task;
}

public sealed record CreateStatusRequest
{
    public EntityType EntityType { get; init; } = EntityType.Task;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = null!;

    [MaxLength(512)]
    public string? Description { get; init; }

    [MaxLength(32)]
    public string? Color { get; init; }

    public StatusCategory Category { get; init; } = StatusCategory.Backlog;
}

public sealed record UpdateStatusRequest
{
    public int Id { get; init; }

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = null!;

    [MaxLength(512)]
    public string? Description { get; init; }

    [MaxLength(32)]
    public string? Color { get; init; }

    public StatusCategory Category { get; init; }
}

public sealed record ReorderStatusesRequest
{
    public EntityType EntityType { get; init; } = EntityType.Task;

    public IReadOnlyList<int> StatusIds { get; init; } = [];
}
