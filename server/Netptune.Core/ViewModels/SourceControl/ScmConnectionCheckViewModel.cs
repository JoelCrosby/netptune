using Netptune.Core.SourceControl.Checks;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record ScmConnectionCheckViewModel
{
    public required string Key { get; init; }

    public ScmConnectionCheckStatus Status { get; init; }

    public bool IsRequired { get; init; }

    public required string Title { get; init; }

    public required string Detail { get; init; }
}
