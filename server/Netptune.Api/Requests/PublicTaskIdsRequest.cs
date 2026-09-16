using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicTaskIdsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<int> TaskIds { get; init; } = [];
}
