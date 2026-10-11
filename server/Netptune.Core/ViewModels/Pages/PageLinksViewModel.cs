namespace Netptune.Core.ViewModels.Pages;

public sealed record PageLinksViewModel
{
    public List<PageLinkedTaskViewModel> Tasks { get; init; } = [];

    public List<PageLinkedPageViewModel> Pages { get; init; } = [];

    public List<PageBacklinkViewModel> Backlinks { get; init; } = [];
}
