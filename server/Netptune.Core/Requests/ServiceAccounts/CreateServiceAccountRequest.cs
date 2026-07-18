namespace Netptune.Core.Requests.ServiceAccounts;

public sealed record CreateServiceAccountRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = [];

    public IReadOnlyList<string> OwnerUserIds { get; init; } = [];
}

public sealed record CreateApiCredentialRequest
{
    public required string Name { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];

    public DateTime? ExpiresAt { get; init; }
}
