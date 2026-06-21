using System.ComponentModel.DataAnnotations;
using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public record UpdateBoardRequest
{
    [Required]
    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? Identifier { get; set; }

    public BoardMeta? Meta { get; set; }
}
