namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectCallback
{
    public int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public required IReadOnlyDictionary<string, string> Parameters { get; init; }
}
