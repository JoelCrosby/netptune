using Netptune.Core.Enums;

namespace Netptune.Core.Events;

public class ActivityMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Activity;

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
    public Guid EventId { get; init; }

    public EntityType EntityType { get; init; }

    public string UserId { get; init; } = null!;

    public ActivityType Type { get; init; }

    public int? EntityId { get; init; }

    public int WorkspaceId { get; init; }

    public TaskChangeField? Field { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    // Old/new values are prefixes (ActivityValue.Truncate). The hashes are set only when the value was
    // actually cut, and are what the merge's no-op check compares instead of the prefixes. Null on values
    // short enough to compare directly, and on messages published before the hashes existed.
    public string? OldValueHash { get; init; }

    public string? NewValueHash { get; init; }

    public DateTime OccurredAt { get; init; }

    public string? Meta { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public List<string>? RecipientUserIds { get; init; }
}
