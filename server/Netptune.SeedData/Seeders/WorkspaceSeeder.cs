using Netptune.Core.Entities;
using Netptune.Core.Meta;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class WorkspaceSeeder : ISeeder
{
    public int Phase => 1;

    private static readonly (string Name, string Slug, string Color, string Description)[] Data =
    [
        ("Acme Corp",        "acme-corp",        "#3b82f6", "Engineering hub for Acme Corp products and services"),
        ("Startup Hub",      "startup-hub",      "#10b981", "Product development workspace for the Startup Hub team"),
        ("Open Source",      "open-source",      "#f59e0b", "Community-driven open source projects and tooling"),
        ("DevOps Platform",  "devops-platform",  "#8b5cf6", "Infrastructure, deployment pipelines, and observability"),
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var owner = context.Users.First(u => u.Email == "joel@netptune.co.uk");

        context.Workspaces.AddRange(Data.Select((w, _) => new Workspace
        {
            Name = w.Name,
            Slug = w.Slug,
            Description = w.Description,
            MetaInfo = new WorkspaceMeta { Color = w.Color },
            Owner = owner,
        }));

        await dbContext.Workspaces.AddRangeAsync(context.Workspaces, ct);
    }
}
