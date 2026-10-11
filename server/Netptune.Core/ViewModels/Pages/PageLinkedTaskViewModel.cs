using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pages;

public sealed record PageLinkedTaskViewModel
{
    public int Id { get; init; }

    public required string SystemId { get; init; }

    public required string Name { get; init; }

    public string? StatusName { get; init; }

    public string? StatusColor { get; init; }

    public StatusCategory? StatusCategory { get; init; }

    public string? SprintName { get; init; }

    public List<string> AssigneeNames { get; init; } = [];
}
