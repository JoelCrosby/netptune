using Netptune.Core.Authorization;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class WorkspaceUserSeeder : ISeeder
{
    public int Phase => 1;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.WorkspaceUsers.AddRange(
            context.Workspaces.SelectMany(workspace => context.Users.Select(user => new WorkspaceAppUser
            {
                User = user,
                Workspace = workspace,
                Role = workspace.Owner == user ? WorkspaceRole.Owner : WorkspaceRole.Member,
                Permissions = WorkspaceRolePermissions.GetDefaultPermissions(
                    workspace.Owner == user ? WorkspaceRole.Owner : WorkspaceRole.Member).ToList(),
            }))
        );

        await dbContext.WorkspaceAppUsers.AddRangeAsync(context.WorkspaceUsers, ct);
    }
}
