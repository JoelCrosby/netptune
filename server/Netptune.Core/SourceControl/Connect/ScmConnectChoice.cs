namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectChoice
{
    public required IReadOnlyList<ScmConnectCandidate> Candidates { get; init; }

    public required string InstallUrl { get; init; }
}
