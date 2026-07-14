using Netptune.Core.Authorization;

namespace Netptune.Core.Requests;

public record UpdateWorkspaceRoleRequest
{
    public string UserId { get; init; } = null!;

    public WorkspaceRole Role { get; init; }
}
