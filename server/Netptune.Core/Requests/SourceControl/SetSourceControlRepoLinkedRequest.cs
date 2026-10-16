namespace Netptune.Core.Requests.SourceControl;

public sealed record SetSourceControlRepoLinkedRequest
{
    public bool IsLinked { get; init; }
}
