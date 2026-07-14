using Netptune.Core.Authorization;

namespace Netptune.Core.ViewModels.Users;

public record WorkspaceRoleUpdateViewModel
{
    public WorkspaceRole Role { get; init; }

    public List<string> Permissions { get; init; } = [];
}
