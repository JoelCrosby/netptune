using Netptune.Core.Enums;

namespace Netptune.Core.Events;

public sealed record ActivityEventPayload
{
    public required ActivityType ActivityType { get; init; }

    public TaskChangeField? Field { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public string? OldValueHash { get; init; }

    public string? NewValueHash { get; init; }

    public string? Meta { get; init; }

    public List<string>? RecipientUserIds { get; init; }

    public string? WorkspaceSlug { get; init; }

    public string? ProjectSlug { get; init; }

    public string? BoardSlug { get; init; }
}
