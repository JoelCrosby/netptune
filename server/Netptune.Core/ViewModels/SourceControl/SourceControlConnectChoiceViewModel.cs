namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlConnectChoiceViewModel
{
    public required List<SourceControlConnectCandidateViewModel> Candidates { get; init; }

    public required string InstallUrl { get; init; }
}
