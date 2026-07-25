namespace Netptune.Core.Events.Automations;

public sealed record AutomationManualRunMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int RuleId { get; init; }

    public List<int> TaskIds { get; init; } = [];

    public string InitiatingUserId { get; init; } = null!;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
