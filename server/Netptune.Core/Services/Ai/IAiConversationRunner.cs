using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiToolInvocationRecord
{
    public required string ToolName { get; init; }

    public required JsonDocument Arguments { get; init; }

    public required string Result { get; init; }

    public bool IsError { get; init; }

    public bool Truncated { get; init; }
}

public sealed record AiRunContext
{
    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public required string ApiKey { get; init; }

    public required string SystemPrompt { get; init; }

    public required IReadOnlyList<AiChatMessage> History { get; init; }

    public required IReadOnlySet<string> Permissions { get; init; }

    public List<AiChatTurn> Turns { get; } = [];

    public List<AiToolInvocationRecord> Invocations { get; } = [];
}

public interface IAiConversationRunner
{
    IAsyncEnumerable<AiStreamEvent> Run(AiRunContext context, CancellationToken cancellationToken);
}
