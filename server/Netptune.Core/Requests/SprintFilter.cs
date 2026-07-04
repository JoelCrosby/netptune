using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record SprintFilter
{
    public int? ProjectId { get; init; }

    public SprintStatus? Status { get; init; }

    public SprintStatus[] Statuses { get; init; } = [];

    public int? Take { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
