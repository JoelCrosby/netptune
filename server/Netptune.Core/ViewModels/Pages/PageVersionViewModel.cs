namespace Netptune.Core.ViewModels.Pages;

public sealed record PageVersionViewModel
{
    public int Id { get; init; }

    public int PageId { get; init; }

    public int Revision { get; init; }

    public required string Title { get; init; }

    public string? Body { get; init; }

    public string? PreviousBody { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime SavedAt { get; init; }

    public string? CreatedByDisplayName { get; init; }
}
