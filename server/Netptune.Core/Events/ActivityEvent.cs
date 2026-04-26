using Netptune.Core.Enums;

namespace Netptune.Core.Events;

public class ActivityMessage : IEventMessage
{
    public List<ActivityEvent> Events { get; init; }

    public ActivityMessage()
    {
        Events = new List<ActivityEvent>();
    }

    public ActivityMessage(ActivityEvent activity)
    {
        Events = new List<ActivityEvent> { activity };
    }

    public ActivityMessage(IEnumerable<ActivityEvent> events)
    {
        Events = events.ToList();
    }
}

public class ActivityEvent
{
    public EntityType EntityType { get; init; }

    public string UserId { get; init; } = null!;

    public ActivityType Type { get; init; }

    public int? EntityId { get; init; }

    public int WorkspaceId { get; init; }

    public DateTime OccurredAt { get; init; }

    public string? Meta { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public List<string>? RecipientUserIds { get; init; }
}
