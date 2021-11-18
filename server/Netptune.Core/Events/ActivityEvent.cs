using System;

using Netptune.Core.Enums;

namespace Netptune.Core.Events;

public class ActivityEvent : IActivityEvent
{
    public EntityType EntityType { get; init; }

    public string UserId { get; init; }

    public ActivityType Type { get; init; }

    public int? EntityId { get; init; }

    public int WorkspaceId { get; init; }

    public DateTime Time { get; init; }

    public string Meta { get; init; }
}