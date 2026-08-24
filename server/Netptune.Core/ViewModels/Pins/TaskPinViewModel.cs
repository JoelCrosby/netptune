using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Pins;

public sealed record TaskPinViewModel
{
    public required int Id { get; init; }

    public required int TaskId { get; init; }

    public required TaskPinScope Scope { get; init; }

    public required int ScopeEntityId { get; init; }

    // Board / project / workspace name, for the chip and the group header.
    public required string ScopeName { get; init; }

    public required double SortOrder { get; init; }

    public required bool CanUnpin { get; init; }

    public required DateTime CreatedAt { get; init; }

    public string? CreatedByUserId { get; init; }
}
