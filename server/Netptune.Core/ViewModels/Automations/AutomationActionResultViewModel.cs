using System.Text.Json;

using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Automations;

public sealed record AutomationActionResultViewModel
{
    public int Id { get; init; }

    public int AutomationActionId { get; init; }

    public AutomationActionType ActionType { get; init; }

    public int SortOrder { get; init; }

    public AutomationActionResultStatus Status { get; init; }

    public string IdempotencyKey { get; init; } = null!;

    public DateTime? StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public string? Message { get; init; }

    public JsonDocument? Output { get; init; }
}
