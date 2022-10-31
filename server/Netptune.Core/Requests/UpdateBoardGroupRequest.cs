using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record UpdateBoardGroupRequest
{
    [Required]
    public int? BoardGroupId { get; set; }

    public string? Name { get; set; }

    public double? SortOrder { get; set; }
}
