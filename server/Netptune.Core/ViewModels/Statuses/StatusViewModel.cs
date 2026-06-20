using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Statuses;

public sealed record StatusViewModel
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public EntityType EntityType { get; init; }

    public string Name { get; init; } = null!;

    public string Key { get; init; } = null!;

    public string? Description { get; init; }

    public string? Color { get; init; }

    public double SortOrder { get; init; }

    public StatusCategory Category { get; init; }

    public bool IsSystem { get; init; }
}
