namespace Netptune.Core.Events.Users;

public class UserPermissionActivityMeta
{
    public string TargetUserId { get; init; } = null!;

    public string Permission { get; init; } = null!;

    public bool Granted { get; init; }
}
