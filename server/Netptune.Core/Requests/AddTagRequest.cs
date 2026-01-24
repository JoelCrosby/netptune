using System;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddTagRequest
{
    [Required]
    public string Tag { get; set; } = null!;
}

public record AddTagToTaskRequest : AddTagRequest
{
    [Required]
    public string SystemId { get; set; } = null!;
}
