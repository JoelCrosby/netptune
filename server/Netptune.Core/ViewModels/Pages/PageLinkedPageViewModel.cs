namespace Netptune.Core.ViewModels.Pages;

public sealed record PageLinkedPageViewModel
{
    public int Id { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }
}
