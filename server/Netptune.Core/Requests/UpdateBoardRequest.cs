using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record UpdateBoardRequest
{
    [Required]
    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? Identifier { get; set; }
}
