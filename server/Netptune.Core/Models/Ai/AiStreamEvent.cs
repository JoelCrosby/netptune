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
}

public sealed record AiStreamEvent
{
    public AiStreamEventType Type { get; init; }

    public string? Text { get; init; }

    public string? ToolName { get; init; }

    public string? Message { get; init; }

    public Guid? ConversationId { get; init; }

    public Guid? ChangeSetId { get; init; }

    public static AiStreamEvent ChangeSetProposed(Guid changeSetId)
    {
        return new AiStreamEvent
        {
            Type = AiStreamEventType.ChangeSetProposed,
            ChangeSetId = changeSetId,
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

    public static AiStreamEvent Failed(string message)
    {
        return new AiStreamEvent { Type = AiStreamEventType.Error, Message = message };
    }
}
