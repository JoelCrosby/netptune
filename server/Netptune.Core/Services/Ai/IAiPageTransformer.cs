using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public enum AiPageTransformAction
{
    Summarise,
    Tighten,
    Translate,
    Continue,
}

public sealed record AiPageTransformRequest
{
    public required AiProvider Provider { get; init; }

    public required string ApiKey { get; init; }

    public required AiPageTransformAction Action { get; init; }

    public required string Text { get; init; }

    public string? Language { get; init; }

    public required string PageTitle { get; init; }
}

public sealed record AiPageTransformResult
{
    public string Text { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public AiUsage Usage { get; init; } = new();
}

public interface IAiPageTransformer
{
    Task<AiPageTransformResult> Transform(AiPageTransformRequest request, CancellationToken cancellationToken);
}
