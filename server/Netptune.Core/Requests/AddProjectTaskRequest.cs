using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public class AddProjectTaskRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [MaxLength(4096)]
    public string Description { get; set; } = null!;

    public ProjectTaskStatus? Status { get; set; }

    public bool IsFlagged { get; set; }

    [Required]
    public int? ProjectId { get; set; }

    public int? BoardGroupId { get; set; }

    public double? SortOrder { get; set; }

    public string? AssigneeId { get; set; }
}
