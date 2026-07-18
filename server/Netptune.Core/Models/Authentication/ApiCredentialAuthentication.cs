namespace Netptune.Core.Models.Authentication;

public sealed record ApiCredentialAuthentication
{
    public Guid CredentialId { get; init; }

    public required byte[] SecretHash { get; init; }

    public required IReadOnlySet<string> Scopes { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; init; }

    public DateTime? LastUsedAt { get; init; }

    public int ServiceAccountId { get; init; }

    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public DateTime? DisabledAt { get; init; }

    public int WorkspaceId { get; init; }

    public required string WorkspaceKey { get; init; }
}
