using Netptune.Core.Statuses;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class StatusSeeder : ISeeder
{
    public int Phase => 1;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var owner = context.Users.FirstOrDefault();

        context.Statuses.AddRange(context.Workspaces.SelectMany(workspace =>
            DefaultTaskStatuses.All.Select(definition =>
            {
                var status = DefaultTaskStatuses.Create(definition, workspace.Id, owner?.Id);
                status.Workspace = workspace;
                status.Owner = owner;
                return status;
            })));

        await dbContext.Statuses.AddRangeAsync(context.Statuses, ct);
    }
}
