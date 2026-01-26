using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddTagRequest
{
    [Required]
    public required string Tag { get; init; }
}

public record AddTagToTaskRequest : AddTagRequest
{
    [Required]
    public required string SystemId { get; init; }
}
