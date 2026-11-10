using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlConnectionViewModel
{
    public int Id { get; init; }

    public SourceControlProvider Provider { get; init; }

    public string? BaseUrl { get; init; }

    public required string AccountLogin { get; init; }

    public ScmAccountType AccountType { get; init; }

    public string? AccountAvatarUrl { get; init; }

    public ScmConnectionStatus Status { get; init; }

    public required List<string> Capabilities { get; init; }

    public int LinkedRepoCount { get; init; }

    public string? CredentialHint { get; init; }

    public DateTime? CredentialExpiresAt { get; init; }

    public bool CanCheck { get; init; }

    public DateTime? LastCheckedAt { get; init; }

    public List<ScmConnectionCheckViewModel> Checks { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
