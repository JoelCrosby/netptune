using Netptune.Core.Authorization;

namespace Netptune.Core.Events.Users;

public class UserRoleActivityMeta
{
    public string TargetUserId { get; init; } = null!;

    public WorkspaceRole OldRole { get; init; }

    public WorkspaceRole NewRole { get; init; }
}
