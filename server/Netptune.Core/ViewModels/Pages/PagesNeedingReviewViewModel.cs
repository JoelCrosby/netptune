namespace Netptune.Core.ViewModels.Pages;

public sealed record PagesNeedingReviewViewModel
{
    public required List<PageNeedingReviewViewModel> Pages { get; init; }

    public int Total { get; init; }
}
