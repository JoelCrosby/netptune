using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

// Each field is "set if provided, otherwise leave unchanged". Sprint and due date have explicit
// Clear flags so they can be removed (null can't distinguish "clear" from "keep"). Tags and
// assignees carry a mode, because a bulk edit is as often "add these" as "make it exactly these".
public class BulkUpdateTasksRequest
{
    public List<int> TaskIds { get; init; } = [];

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public DateOnly? DueDate { get; init; }

    public bool ClearDueDate { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public BulkCollectionMode AssigneeMode { get; init; }

    public List<string>? Tags { get; init; }

    public BulkCollectionMode TagMode { get; init; }
}
