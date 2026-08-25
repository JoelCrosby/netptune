using System.ComponentModel.DataAnnotations;

namespace Netptune.Api.Requests;

public sealed record PublicTaskIdsRequest
{
    [Required]
    [MinLength(1)]
    public List<int> TaskIds { get; init; } = [];
}
