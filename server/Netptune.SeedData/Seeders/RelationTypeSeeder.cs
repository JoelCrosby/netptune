using Netptune.Core.Relations;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class RelationTypeSeeder : ISeeder
{
    public int Phase => 1;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var owner = context.Users.FirstOrDefault();

        context.RelationTypes.AddRange(context.Workspaces.SelectMany(workspace =>
            DefaultRelationTypes.All.Select(definition =>
            {
                var relationType = DefaultRelationTypes.Create(definition, workspace.Id, owner?.Id);
                relationType.Workspace = workspace;
                relationType.Owner = owner;
                return relationType;
            })));

        await dbContext.RelationTypes.AddRangeAsync(context.RelationTypes, ct);
    }
}
