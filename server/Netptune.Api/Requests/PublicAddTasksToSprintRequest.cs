using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicAddTasksToSprintRequest
{
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<string> SystemIds { get; init; } = [];
}
