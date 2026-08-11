using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record AddProjectTaskRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [MaxLength(4096)]
    public string Description { get; set; } = null!;

    public int? StatusId { get; set; }

    [Required]
    public int? ProjectId { get; set; }

    public int? BoardGroupId { get; set; }

    public int? SprintId { get; set; }

    public double? SortOrder { get; set; }

    public string? AssigneeId { get; set; }

    public List<string>? AssigneeIds { get; set; }

    public List<string>? Tags { get; set; }

    public List<AddTaskRelationRequest>? Relations { get; set; }

    public TaskPriority? Priority { get; set; }

    public EstimateType? EstimateType { get; set; }

    public decimal? EstimateValue { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }
}
