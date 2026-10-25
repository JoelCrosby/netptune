using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Connect;

internal sealed record ScmConnectState
{
    public SourceControlProvider Provider { get; init; }

    public int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public string? UserAccessToken { get; init; }
}
