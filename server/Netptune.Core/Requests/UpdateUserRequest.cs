using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record UpdateUserRequest
{
    [Required]
    public string? Id { get; init; }

    public string? Firstname { get; init; }

    public string? Lastname { get; init; }

    public string? PictureUrl { get; init; }
}
