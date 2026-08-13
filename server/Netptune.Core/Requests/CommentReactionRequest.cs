using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public sealed record CommentReactionRequest
{
    [Required]
    public string Value { get; init; } = null!;
}
