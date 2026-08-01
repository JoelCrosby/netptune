using Netptune.Core.Enums;

namespace Netptune.Core.Requests.Ai;

public sealed record SaveAiCredentialRequest
{
    public AiProvider Provider { get; init; }

    public required string Label { get; init; }

    public required string Secret { get; init; }

    public string? Model { get; init; }
}
