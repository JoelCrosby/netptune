using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.PublicApi.Requests;

public sealed record PublicBulkUpdateTasksRequest
{
    public List<int> TaskIds { get; init; } = [];

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public BulkUpdateTasksRequest ToRequest()
    {
        return new BulkUpdateTasksRequest
        {
            TaskIds = TaskIds,
            StatusId = StatusId,
            Priority = Priority,
            EstimateType = EstimateType,
            EstimateValue = EstimateValue,
            SprintId = SprintId,
            ClearSprint = ClearSprint,
            AssigneeIds = AssigneeIds,
        };
    }
}
