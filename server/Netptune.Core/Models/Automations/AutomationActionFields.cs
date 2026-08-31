using Netptune.Core.Authorization;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

// One automation action's configurable fields. The request the client sends and the view model it
// reads back are the same shape, so they share it rather than restating it; the view model adds only
// its identity and ordering on top.
public abstract record AutomationActionFields
{
    public AutomationActionType Type { get; init; }

    public string? Message { get; init; }

    public List<AutomationNotificationRecipient> Recipients { get; init; } = [];

    public List<string> RecipientUserIds { get; init; } = [];

    public List<WorkspaceRole> RecipientRoles { get; init; } = [];

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

    public bool CopyAssignees { get; init; }

    public int? LinkRelationTypeId { get; init; }

    public AutomationRelationOperation? RelationOperation { get; init; }

    public AutomationRelationDirection? RelationDirection { get; init; }

    public int? RelationTypeId { get; init; }

    public int? RelatedTaskId { get; init; }

    public int? DelayAmount { get; init; }

    public AutomationDelayUnit? DelayUnit { get; init; }
}
