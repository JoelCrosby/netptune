using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Handlers.Pins;

public sealed record PinWriteRights
{
    public required bool Board { get; init; }

    public required bool Project { get; init; }

    public required bool Workspace { get; init; }

    public bool For(TaskPinScope scope) => scope switch
    {
        TaskPinScope.Board => Board,
        TaskPinScope.Project => Project,
        TaskPinScope.Workspace => Workspace,
        _ => true,
    };
}

public static class PinsPermissions
{
    public static async Task<PinWriteRights> GetWriteRights(IWorkspacePermissionCache permissionCache, string userId, string? workspaceKey)
    {
        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        return new PinWriteRights
        {
            Board = Allows(permissions, TaskPinScope.Board),
            Project = Allows(permissions, TaskPinScope.Project),
            Workspace = Allows(permissions, TaskPinScope.Workspace),
        };
    }

    public static async Task<bool> CanWrite(
        IWorkspacePermissionCache permissionCache,
        string userId,
        string? workspaceKey,
        TaskPinScope scope)
    {
        if (scope == TaskPinScope.User)
        {
            return true;
        }

        var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

        return Allows(permissions, scope);
    }

    private static bool Allows(UserPermissions? permissions, TaskPinScope scope)
    {
        if (permissions is null)
        {
            return false;
        }

        if (permissions.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
        {
            return true;
        }

        var required = RequiredPermission(scope);

        if (required is null)
        {
            return true;
        }

        return permissions.Permissions.Contains(required);
    }

    private static string? RequiredPermission(TaskPinScope scope) => scope switch
    {
        TaskPinScope.Board => NetptunePermissions.Boards.Update,
        TaskPinScope.Project => NetptunePermissions.Projects.Update,
        TaskPinScope.Workspace => NetptunePermissions.Tasks.PinWorkspace,
        _ => null,
    };
}
