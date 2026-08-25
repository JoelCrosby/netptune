using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicAddCommentRequest
{
    [Required]
    public string Comment { get; init; } = null!;

    public List<string> Mentions { get; init; } = [];

    public AddCommentRequest ToRequest(string systemId)
    {
        return new AddCommentRequest
        {
            SystemId = systemId,
            Comment = Comment,
            Mentions = Mentions,
        };
    }
}
