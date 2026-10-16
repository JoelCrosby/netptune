namespace Netptune.Core.SourceControl.Events;

public enum ScmConnectionAction
{
    Suspended = 0,
    Unsuspended = 1,
    Revoked = 2,
    PermissionsChanged = 3,
    RepositoriesAdded = 4,
    RepositoriesRemoved = 5,
}
