using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicBulkUpdateTasksRequest
{
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<int> TaskIds { get; init; } = [];

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public DateOnly? DueDate { get; init; }

    public bool ClearDueDate { get; init; }

    [MaxLength(RequestLimits.MaxBulkAssignees)]
    public List<string>? AssigneeIds { get; init; }

    public BulkCollectionMode AssigneeMode { get; init; }

    [MaxLength(RequestLimits.MaxBulkTags)]
    public List<string>? Tags { get; init; }

    public BulkCollectionMode TagMode { get; init; }

    public int? BoardGroupId { get; init; }

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
            DueDate = DueDate,
            ClearDueDate = ClearDueDate,
            AssigneeIds = AssigneeIds,
            AssigneeMode = AssigneeMode,
            Tags = Tags,
            TagMode = TagMode,
        };
    }
}
