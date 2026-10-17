using Netptune.Core.SourceControl;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record ScmCheckSummaryViewModel
{
    public ScmCheckState State { get; init; }

    public int Total { get; init; }

    public int Passed { get; init; }

    public int Failed { get; init; }

    public int Pending { get; init; }

    public string? AttentionUrl { get; init; }
}
