using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiToolInvocationRecord
{
    public required string ToolName { get; init; }

    // What the work log shows for the call; see IAiTool.DescribeCall.
    public string? Label { get; init; }

    public required JsonDocument Arguments { get; init; }

    public required string Result { get; init; }

    public bool IsError { get; init; }

    public bool Truncated { get; init; }
}

public sealed record AiRunContext
{
    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public AiEffort? Effort { get; init; }

    public required string ApiKey { get; init; }

    public required string SystemPrompt { get; init; }

    public required IReadOnlyList<AiChatMessage> History { get; init; }

    public required IReadOnlySet<string> Permissions { get; init; }

    public decimal? SpendRemaining { get; init; }

    public IReadOnlySet<AiToolKind> ToolKinds { get; init; } = AllToolKinds;

    // The correction is for chat replies that claim a proposal no tool made. Other callers write their own
    // replies, where wording like "pending approval" means something else.
    public bool CorrectsProposalClaims { get; init; } = true;

    public int? MaxToolIterations { get; init; }

    public int? MaxOutputTokens { get; init; }

    public int? MaxTurnCharacters { get; init; }

    public List<AiChatTurn> Turns { get; } = [];

    public List<AiToolInvocationRecord> Invocations { get; } = [];

    private static readonly IReadOnlySet<AiToolKind> AllToolKinds = Enum.GetValues<AiToolKind>().ToHashSet();
}

public interface IAiConversationRunner
{
    IAsyncEnumerable<AiStreamEvent> Run(AiRunContext context, CancellationToken cancellationToken);
}
