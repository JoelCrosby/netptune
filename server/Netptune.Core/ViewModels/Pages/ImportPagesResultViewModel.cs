namespace Netptune.Core.ViewModels.Pages;

public sealed record ImportPagesResultViewModel
{
    public int Created { get; init; }

    public List<string> Skipped { get; init; } = [];

    public string? FirstSlug { get; init; }
}
