namespace Netptune.Core.SourceControl.Checks;

public sealed record ScmConnectionCheck
{
    public required string Key { get; init; }

    public ScmConnectionCheckStatus Status { get; init; }

    public Dictionary<string, string> Facts { get; init; } = [];
}
