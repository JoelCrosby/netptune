using Netptune.Core.SourceControl;

namespace Netptune.Core.Requests.SourceControl;

public sealed record CompleteSourceControlConnectRequest
{
    public SourceControlProvider Provider { get; init; }

    public Dictionary<string, string> Parameters { get; init; } = [];
}
