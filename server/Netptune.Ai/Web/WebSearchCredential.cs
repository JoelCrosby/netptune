using Netptune.Core.Enums;

namespace Netptune.Ai.Web;

public sealed record WebSearchCredential
{
    public WebSearchProvider Provider { get; init; }

    public string? ApiKey { get; init; }

    public string? EngineId { get; init; }

    public string? Endpoint { get; init; }
}
