namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmConnectCandidate
{
    public required string ExternalAccountId { get; init; }

    public required ScmAccount Account { get; init; }

    public required IReadOnlyDictionary<string, string> CallbackParameters { get; init; }
}
