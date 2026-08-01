using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiTitleRequest
{
    public AiProvider Provider { get; init; }

    public required string ApiKey { get; init; }

    public required string UserMessage { get; init; }

    public required string AssistantMessage { get; init; }
}

public sealed record AiTitleResult
{
    public string? Title { get; init; }

    public AiUsage Usage { get; init; } = new();
}

public interface IAiTitleGenerator
{
    Task<AiTitleResult> Generate(AiTitleRequest request, CancellationToken cancellationToken);
}
