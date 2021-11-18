using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public class AddCommentRequest
{
    [Required]
    public string Comment { get; set; }

    [Required]
    public string SystemId { get; set; }
}