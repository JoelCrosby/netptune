using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddCommentRequest
{
    [Required]
    public string Comment { get; set; } = null!;

    [Required]
    public string SystemId { get; set; } = null!;

    public List<string> Mentions { get; set; } = new();
}

public sealed record UpdateCommentRequest
{
    [Required]
    public string Comment { get; init; } = null!;

    public List<string> Mentions { get; init; } = [];
}
