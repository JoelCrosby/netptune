namespace Netptune.Core.SourceControl.Connect;

public sealed record ScmAccount
{
    public required string ExternalId { get; init; }

    public required string Login { get; init; }

    public ScmAccountType AccountType { get; init; }

    public string? AvatarUrl { get; init; }
}
