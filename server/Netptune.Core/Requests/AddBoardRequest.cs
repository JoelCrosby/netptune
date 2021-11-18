using System.ComponentModel.DataAnnotations;
using Netptune.Core.Meta;

namespace Netptune.Core.Requests;

public class AddBoardRequest
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Identifier { get; set; }

    [Required]
    public int? ProjectId { get; set; }

    public BoardMeta Meta { get; set; }
}