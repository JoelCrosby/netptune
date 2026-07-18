namespace Netptune.Core.ViewModels.ServiceAccounts;

public sealed record ServiceAccountViewModel
{
    public int Id { get; init; }

    public required string UserId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? DisabledAt { get; init; }

    public required IReadOnlyList<string> OwnerUserIds { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }

    public IReadOnlyList<ApiCredentialViewModel> Credentials { get; init; } = [];
}

public sealed record ApiCredentialViewModel
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string TokenPrefix { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; init; }

    public DateTime? LastUsedAt { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }
}

public sealed record ApiCredentialCreatedViewModel
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Token { get; init; }

    public DateTime ExpiresAt { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }
}
