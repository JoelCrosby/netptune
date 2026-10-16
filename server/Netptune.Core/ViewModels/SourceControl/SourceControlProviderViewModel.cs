using Netptune.Core.SourceControl;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlProviderViewModel
{
    public SourceControlProvider Provider { get; init; }

    public required ScmTerminology Terminology { get; init; }

    public required List<string> Capabilities { get; init; }
}
