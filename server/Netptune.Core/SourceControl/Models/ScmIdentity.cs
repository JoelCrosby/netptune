namespace Netptune.Core.SourceControl.Models;

public sealed record ScmIdentity
{
    public SourceControlProvider Provider { get; init; }

    public string? ExternalId { get; init; }

    public required string Username { get; init; }

    public string? DisplayName { get; init; }

    public string? Email { get; init; }

    public string? AvatarUrl { get; init; }
}
