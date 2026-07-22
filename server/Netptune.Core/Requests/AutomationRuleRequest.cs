using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.Requests;

public record AutomationRuleRequest
{
    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; } = true;

    public AutomationTriggerRequest Trigger { get; init; } = null!;

    public List<AutomationActionRequest> Actions { get; init; } = [];
}

public record AutomationTriggerRequest
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public List<AutomationFieldCondition>? Conditions { get; init; }

    public int? StatusId { get; init; }

    public AssigneeChangeMode? AssigneeChangeMode { get; init; }

    public int? DurationDays { get; init; }
}

public record AutomationActionRequest
{
    public AutomationActionType Type { get; init; }

    public string? Message { get; init; }

    public string? Comment { get; init; }

    public string? FlagName { get; init; }

    public string? FlagDescription { get; init; }

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public int? DelayAmount { get; init; }

    public AutomationDelayUnit? DelayUnit { get; init; }
}
