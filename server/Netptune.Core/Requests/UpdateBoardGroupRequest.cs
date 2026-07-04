using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record UpdateBoardGroupRequest
{
    [Required]
    public int? BoardGroupId { get; set; }

    public string? Name { get; set; }

    public double? SortOrder { get; set; }

    public int? StatusId { get; set; }

    // Distinguishes "leave the assigned status unchanged" (StatusId null, ClearStatus false)
    // from "unassign the status" (ClearStatus true).
    public bool ClearStatus { get; set; }
}
