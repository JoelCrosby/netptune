using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.ViewModels.Ai;

public sealed record AiTokenUsageViewModel
{
    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }

    // Estimated spend in US dollars, priced from the published rates for the model that produced
    // the tokens.
    public decimal Cost { get; init; }

    public AiTokenUsageViewModel WithCost(string? model)
    {
        return this with
        {
            Cost = AiModelPricing.Cost(model, InputTokens, OutputTokens, CacheReadTokens, CacheCreationTokens),
        };
    }

    public static AiTokenUsageViewModel From(AiUsage usage)
    {
        return new AiTokenUsageViewModel
        {
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            CacheReadTokens = usage.CacheReadTokens,
            CacheCreationTokens = usage.CacheCreationTokens,
        };
    }
}

public sealed record AiConversationViewModel
{
    public Guid Id { get; init; }

    public required string Title { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public string? RequestedModel { get; init; }

    public AiEffort? RequestedEffort { get; init; }

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

    public Guid? ChangeSetId { get; init; }

    public AiQuestion? Question { get; init; }

    public AiQuestionAnswer? Answer { get; init; }

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
