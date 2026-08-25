using System.ComponentModel.DataAnnotations;

namespace Netptune.Api.Requests;

public sealed record PublicRenameTagRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(128)]
    public string NewValue { get; init; } = null!;
}
