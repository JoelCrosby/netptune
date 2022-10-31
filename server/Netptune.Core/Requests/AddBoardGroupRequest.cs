using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record AddBoardGroupRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public int? BoardId { get; set; }

    public BoardGroupType? Type { get; set; }

    public double? SortOrder { get; set; }
}
