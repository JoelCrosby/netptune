namespace Netptune.Core.Models.Ai;

public sealed record AiCompactedHistory
{
    public required List<AiChatMessage> Messages { get; init; }

    public int DroppedMessages { get; init; }

    public List<string> DroppedQuestions { get; init; } = [];
}
