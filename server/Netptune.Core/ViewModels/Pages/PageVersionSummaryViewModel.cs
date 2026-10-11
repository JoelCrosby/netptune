namespace Netptune.Core.ViewModels.Pages;

public sealed record PageVersionSummaryViewModel
{
    public int Id { get; init; }

    public int Revision { get; init; }

    public required string Title { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime SavedAt { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public int? RestoredFromVersionId { get; init; }

    public DateTime? RestoredFromCreatedAt { get; init; }
}
