using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class TagSeeder : ISeeder
{
    public int Phase => 2;

    private static readonly string[] Names =
    [
        "backend", "frontend", "mobile", "infrastructure", "security",
        "performance", "accessibility", "testing", "documentation", "bug",
        "feature", "tech-debt", "breaking-change", "urgent",
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Tags.AddRange(
            context.Workspaces.SelectMany(workspace =>
            {
                var workspaceUsers = context.UsersFor(workspace);

                return Names.Select((name, i) => new Tag
                {
                    Name = name,
                    Owner = workspaceUsers[i % workspaceUsers.Count],
                    Workspace = workspace,
                });
            })
        );

        await dbContext.Tags.AddRangeAsync(context.Tags, ct);
    }
}
