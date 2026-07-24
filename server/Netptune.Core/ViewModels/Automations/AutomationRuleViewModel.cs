using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.ViewModels.Automations;

public record AutomationRuleViewModel
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; }

    public string? ExecutionUserId { get; init; }

    public AutomationTriggerViewModel Trigger { get; init; } = null!;

    public List<AutomationActionViewModel> Actions { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public record AutomationTriggerViewModel
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public AutomationConditionGroup? ConditionGroup { get; init; }

    public int? DurationDays { get; init; }
}

public record AutomationActionViewModel
{
    public int Id { get; init; }

    public AutomationActionType Type { get; init; }

    public int SortOrder { get; init; }

    public string? Message { get; init; }

    public string? Comment { get; init; }

    public string? FlagName { get; init; }

    public string? FlagDescription { get; init; }

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public string? TaskName { get; init; }

    public string? TaskDescription { get; init; }

    public bool ClearDescription { get; init; }

    public string? OwnerId { get; init; }

    public bool ClearOwner { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public List<string> AddTags { get; init; } = [];

    public List<string> RemoveTags { get; init; } = [];

    public AutomationDateUpdate? StartDate { get; init; }

    public AutomationDateUpdate? DueDate { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public bool ClearEstimate { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public int? BoardGroupId { get; init; }

    public int? DelayAmount { get; init; }

    public AutomationDelayUnit? DelayUnit { get; init; }
}
