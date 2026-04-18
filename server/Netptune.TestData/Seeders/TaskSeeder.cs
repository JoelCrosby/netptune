using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class TaskSeeder
{
    private static readonly string[] Names =
    [
        "Migrate authentication flow to use refresh token rotation",
        "Add OpenTelemetry tracing to all API endpoints",
        "Refactor component state management to NgRx signal store",
        "Implement database connection pooling for high-load endpoints",
        "Add end-to-end tests for the checkout workflow",
        "Extract shared UI components into a standalone component library",
        "Configure Kubernetes horizontal pod autoscaling for the API",
        "Upgrade Entity Framework Core and resolve breaking changes",
    ];

    private static readonly string[] Descriptions =
    [
        "Current implementation stores tokens in localStorage. Switch to httpOnly cookies with refresh token rotation to meet security compliance requirements.",
        "Instrument all controllers and service boundaries with activity spans. Export traces to the collector endpoint configured in app settings.",
        "The component uses a mix of BehaviorSubjects and local state. Consolidate into a feature store with selectors for all derived state.",
        "API endpoints are creating new DbContext instances per request without pooling. Configure AddDbContextPool with an appropriate pool size.",
        "The checkout workflow has no automated coverage. Add Playwright e2e tests for the happy path, payment failure, and session timeout scenarios.",
        "Several feature modules duplicate button, input, and modal components. Extract into a shared library with a consistent design token system.",
        "The API deployment scales manually. Define HPA rules based on CPU and request queue depth metrics from the Prometheus scrape endpoint.",
        "EF Core 9 introduces breaking changes to the query pipeline and owned entity mapping. Resolve migration conflicts and update affected raw SQL queries.",
    ];

    private static readonly ProjectTaskStatus[] Statuses = Enum.GetValues<ProjectTaskStatus>();

    internal static List<ProjectTask> Generate(List<AppUser> users, List<Project> projects) =>
        projects
            .SelectMany((project, pi) => Enumerable.Range(0, 8).Select(i => new ProjectTask
            {
                Name = Names[i],
                Description = Descriptions[i],
                Status = Statuses[(pi * 8 + i) % Statuses.Length],
                IsFlagged = (pi * 8 + i) % 2 == 0,
                Owner = users[(pi + i) % users.Count],
                Project = project,
                ProjectScopeId = i,
                Workspace = project.Workspace,
            }))
            .ToList();
}
