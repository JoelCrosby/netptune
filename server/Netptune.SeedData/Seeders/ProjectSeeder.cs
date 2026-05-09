using Netptune.Core.Entities;
using Netptune.Core.Meta;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class ProjectSeeder : ISeeder
{
    public int Phase => 1;

    private static readonly (string Name, string Key, string WorkspaceSlug, string Description, string Color)[] Data =
    [
        ("Platform API",      "PAT", "acme-corp",       "REST and GraphQL APIs powering the Acme Corp platform",               "#3b82f6"),
        ("Mobile App",        "MOB", "acme-corp",       "iOS and Android client for the Acme Corp platform",                   "#6366f1"),
        ("Dashboard",         "DSH", "startup-hub",     "Analytics and reporting dashboard for key product metrics",           "#10b981"),
        ("Marketing Site",    "MKT", "startup-hub",     "Public-facing marketing site and SEO-optimised landing pages",        "#14b8a6"),
        ("CLI Tools",         "CLI", "open-source",     "Developer CLI tools for scaffolding and workspace management",        "#f59e0b"),
        ("Component Library", "CMP", "open-source",     "Shared UI component library with accessibility-first design",        "#f97316"),
        ("Infrastructure",    "INF", "devops-platform",  "Kubernetes clusters, Terraform modules, and cloud configuration",    "#8b5cf6"),
        ("Monitoring",        "MON", "devops-platform",  "Observability stack: metrics, tracing, alerting, and dashboards",    "#ec4899"),
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Projects.AddRange(Data.Select((p, i) => new Project
        {
            Name = p.Name,
            Key = p.Key,
            Description = p.Description,
            MetaInfo = new ProjectMeta { Color = p.Color },
            Owner = context.Users[i % context.Users.Count],
            Workspace = context.Workspaces.First(w => w.Slug == p.WorkspaceSlug),
        }));

        await dbContext.Projects.AddRangeAsync(context.Projects, ct);
    }
}
