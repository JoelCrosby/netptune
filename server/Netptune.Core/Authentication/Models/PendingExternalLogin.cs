namespace Netptune.Core.Authentication.Models;

public sealed class PendingExternalLogin
{
    public required string ExistingUserId { get; init; }

    public required string Provider { get; init; }

    public required string ProviderKey { get; init; }

    public required string Email { get; init; }

    public string? DisplayName { get; init; }

    public string? PictureUrl { get; init; }

    public DateTime Created { get; init; }
}
