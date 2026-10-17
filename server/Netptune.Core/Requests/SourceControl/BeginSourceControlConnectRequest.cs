using Netptune.Core.SourceControl;

namespace Netptune.Core.Requests.SourceControl;

public sealed record BeginSourceControlConnectRequest
{
    public SourceControlProvider Provider { get; init; }
}
