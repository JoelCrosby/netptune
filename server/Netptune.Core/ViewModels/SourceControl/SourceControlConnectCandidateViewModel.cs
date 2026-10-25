using Netptune.Core.SourceControl.Connect;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlConnectCandidateViewModel
{
    public required string AccountLogin { get; init; }

    public ScmAccountType AccountType { get; init; }

    public string? AccountAvatarUrl { get; init; }

    public required Dictionary<string, string> CallbackParameters { get; init; }
}
