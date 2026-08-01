using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.ViewModels.Ai;

public sealed record AiTokenUsageViewModel
{
    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }
}

public sealed record AiConversationViewModel
{
    public Guid Id { get; init; }

    public required string Title { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public DateTime LastMessageAt { get; init; }

    public int MessageCount { get; init; }

    public AiTokenUsageViewModel Usage { get; init; } = new();
}

public sealed record AiMessageViewModel
{
    public long Id { get; init; }

    public int Sequence { get; init; }

    public AiMessageRole Role { get; init; }

    public string? Text { get; init; }

    public List<string> ToolNames { get; init; } = [];

    public List<AiEntityReference> References { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}

public sealed record AiWorkspaceConversationViewModel
{
    public Guid Id { get; init; }

    public required string Title { get; init; }

    public required string UserId { get; init; }

    public required string UserDisplayName { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public DateTime LastMessageAt { get; init; }

    public int MessageCount { get; init; }

    public AiTokenUsageViewModel Usage { get; init; } = new();
}

public sealed record AiConversationDetailViewModel
{
    public required AiConversationViewModel Conversation { get; init; }

    public List<AiMessageViewModel> Messages { get; init; } = [];

    public AiChangeSetViewModel? PendingChangeSet { get; init; }
}
