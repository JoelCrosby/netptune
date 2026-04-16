using Netptune.Core.Authorization;

namespace Netptune.Core.Models;

public class UserPermissions
{
    public required string UserId { get; init; }

    public required string WorkspaceKey { get; init; }

    public required WorkspaceRole Role { get; init; }

    public required HashSet<string> Permissions { get; init; }
}
