using Netptune.Core.Authorization;

namespace Netptune.Core.Requests;

public sealed class WorkspaceUserFilter : PageRequest
{
    public string? Search { get; init; }

    public WorkspaceRole? Role { get; init; }

    public bool? IsPending { get; init; }
}
