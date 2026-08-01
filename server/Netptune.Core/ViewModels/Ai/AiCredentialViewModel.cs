using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Ai;

public sealed record AiCredentialViewModel
{
    public Guid Id { get; init; }

    public AiProvider Provider { get; init; }

    public required string Label { get; init; }

    public required string SecretHint { get; init; }

    public string? Model { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; init; }
}
