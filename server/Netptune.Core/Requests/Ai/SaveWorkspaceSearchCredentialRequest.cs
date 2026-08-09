using Netptune.Core.Enums;

namespace Netptune.Core.Requests.Ai;

public sealed record SaveWorkspaceSearchCredentialRequest
{
    public WebSearchProvider Provider { get; init; }

    // Left null on an edit that is not changing the key, so the stored secret is kept.
    public string? Secret { get; init; }

    public string? EngineId { get; init; }

    public string? Endpoint { get; init; }
}
