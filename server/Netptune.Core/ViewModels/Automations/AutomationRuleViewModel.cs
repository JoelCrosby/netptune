using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Automations;

public record AutomationRuleViewModel
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; }

    public AutomationTriggerViewModel Trigger { get; init; } = null!;

    public List<AutomationActionViewModel> Actions { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public record AutomationTriggerViewModel
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public ProjectTaskStatus? Status { get; init; }

    public AssigneeChangeMode? AssigneeChangeMode { get; init; }

    public int? DurationDays { get; init; }
}

public record AutomationActionViewModel
{
    public int Id { get; init; }

    public AutomationActionType Type { get; init; }

    public int SortOrder { get; init; }

    public string? Message { get; init; }

    public string? FlagName { get; init; }

    public string? FlagDescription { get; init; }

    public ProjectTaskStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }
}
