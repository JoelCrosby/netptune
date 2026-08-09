using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Models.Ai;

public enum AiStreamEventType
{
    TextDelta = 0,
    ToolStarted = 1,
    ToolCompleted = 2,
    TurnCompleted = 3,
    Error = 4,
    ConversationStarted = 5,
    ChangeSetProposed = 6,
    EntitiesReferenced = 7,
    ReplyReset = 8,
    Stopped = 9,
    HistoryCompacted = 10,
    UsageUpdated = 11,
    TurnUsage = 12,
}

public sealed record AiStreamEvent
{
    public AiStreamEventType Type { get; init; }

    public string? Text { get; init; }

    public string? ToolName { get; init; }

    public string? Message { get; init; }

    public Guid? ConversationId { get; init; }

    public Guid? ChangeSetId { get; init; }

    public List<AiEntityReference>? References { get; init; }

    public int? DroppedMessages { get; init; }

    public AiTokenUsageViewModel? Usage { get; init; }

    public static AiStreamEvent UsageUpdated(AiTokenUsageViewModel usage)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.UsageUpdated,
            Usage = usage,
        };
    }

    // Tokens the turn has spent so far, which the client counts up while it waits.
    // The conversation total only follows once the turn is stored.
    public static AiStreamEvent TurnUsage(AiTokenUsageViewModel usage)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.TurnUsage,
            Usage = usage,
        };
    }

    public static AiStreamEvent ChangeSetProposed(Guid changeSetId)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.ChangeSetProposed,
            ChangeSetId = changeSetId,
        };
    }

    public static AiStreamEvent EntitiesReferenced(List<AiEntityReference> references)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.EntitiesReferenced,
            References = references,
        };
    }

    public static AiStreamEvent ConversationStarted(Guid conversationId)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.ConversationStarted,
            ConversationId = conversationId,
        };
    }

    public static AiStreamEvent Delta(string text)
    {
        return new AiStreamEvent { Type = AiStreamEventType.TextDelta, Text = text };
    }

    public static AiStreamEvent ToolStarted(string toolName)
    {
        return new AiStreamEvent { Type = AiStreamEventType.ToolStarted, ToolName = toolName };
    }

    public static AiStreamEvent ToolCompleted(string toolName)
    {
        return new AiStreamEvent { Type = AiStreamEventType.ToolCompleted, ToolName = toolName };
    }

    public static AiStreamEvent ReplyReset()
    {
        return new AiStreamEvent { Type = AiStreamEventType.ReplyReset };
    }

    public static AiStreamEvent HistoryCompacted(int droppedMessages)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.HistoryCompacted,
            DroppedMessages = droppedMessages,
        };
    }

    public static AiStreamEvent Stopped()
    {
        return new AiStreamEvent { Type = AiStreamEventType.Stopped };
    }

    public static AiStreamEvent Failed(string message)
    {
        return new AiStreamEvent { Type = AiStreamEventType.Error, Message = message };
    }
}
