namespace Netptune.Core.Models.Ai;

public static class AiSpendMessages
{
    public const string CapReached = "This workspace has reached its monthly assistant spend cap.";
}

public sealed record AiSpendSlice
{
    public required string UserId { get; init; }

    public required string UserDisplayName { get; init; }

    public required string Model { get; init; }

    public DateTime Day { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }
}

public sealed record AiModelTokens
{
    public required string Model { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }
}

public sealed record AiConversationCount(string UserId, int Count);

public sealed record AiSpendStatus
{
    public decimal MonthToDate { get; init; }

    public decimal? Cap { get; init; }

    public bool IsOverCap => Cap.HasValue && MonthToDate >= Cap.Value;
}
