using Netptune.Core.SourceControl;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlIdentityViewModel
{
    public int Id { get; init; }

    public SourceControlProvider Provider { get; init; }

    public string? Username { get; init; }

    public bool IsVerified { get; init; }
}
