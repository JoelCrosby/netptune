using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

// Each field is "set if provided, otherwise leave unchanged". Sprint has an explicit
// ClearSprint flag so it can be removed (null can't distinguish "clear" from "keep").
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

    public List<string>? AssigneeIds { get; init; }
}
