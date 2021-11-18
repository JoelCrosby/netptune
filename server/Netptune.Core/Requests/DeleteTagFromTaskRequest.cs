using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public class DeleteTagFromTaskRequest
{
    [Required]
    public string SystemId { get; set; }

    [Required]
    public string Tag { get; set; }
}