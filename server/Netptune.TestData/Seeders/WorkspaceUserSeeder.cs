using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.TestData.Seeders;

internal static class WorkspaceUserSeeder
{
    internal static List<WorkspaceAppUser> Generate(List<Workspace> workspaces, List<AppUser> users) =>
        workspaces
            .SelectMany(workspace => users.Select(user => new WorkspaceAppUser
            {
                User = user,
                Workspace = workspace,
                Role = workspace.Owner == user ? WorkspaceRole.Owner : WorkspaceRole.Member,
                Permissions = WorkspaceRolePermissions.GetDefaultPermissions(
                    workspace.Owner == user ? WorkspaceRole.Owner : WorkspaceRole.Member).ToList(),
            }))
            .ToList();
}
