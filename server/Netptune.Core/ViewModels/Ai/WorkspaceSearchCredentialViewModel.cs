using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Ai;

public sealed record WorkspaceSearchCredentialViewModel
{
    public Guid Id { get; init; }

    public WebSearchProvider Provider { get; init; }

    public required string SecretHint { get; init; }

    public string? EngineId { get; init; }

    public string? Endpoint { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; init; }
}
