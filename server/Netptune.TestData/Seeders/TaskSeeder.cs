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

    internal static List<ProjectTask> Generate(List<AppUser> users, List<Project> projects, List<Status> statuses)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return projects
            .SelectMany((project, projectIndex) =>
            {
                var taskStatuses = statuses
                    .Where(status => status.Workspace == project.Workspace && status.EntityType == EntityType.Task)
                    .OrderBy(status => status.SortOrder)
                    .ThenBy(status => status.Id)
                    .ToList();

                return Enumerable.Range(0, 8).Select(taskIndex =>
                {
                    var schedule = GetSchedule(today, projectIndex, taskIndex);

                    return new ProjectTask
                    {
                        Status = taskStatuses[(projectIndex * 8 + taskIndex) % taskStatuses.Count],
                        Name = Names[taskIndex],
                        Description = Descriptions[taskIndex],
                        Owner = users[(projectIndex + taskIndex) % users.Count],
                        Project = project,
                        ProjectScopeId = taskIndex,
                        Workspace = project.Workspace,
                        StartDate = schedule.StartDate,
                        DueDate = schedule.DueDate,
                    };
                });
            })
            .ToList();
    }

    private static (DateOnly? StartDate, DateOnly? DueDate) GetSchedule(DateOnly today, int projectIndex, int taskIndex)
    {
        var projectOffset = projectIndex % 4 * 3;
        var anchor = today.AddDays(projectOffset);

        return taskIndex switch
        {
            0 => (anchor.AddDays(-21), anchor.AddDays(-7)),
            1 => (anchor.AddDays(-10), anchor.AddDays(4)),
            2 => (anchor, anchor.AddDays(14)),
            3 => (null, anchor.AddDays(5)),
            4 => (anchor.AddDays(7), anchor.AddDays(28)),
            5 => (anchor.AddDays(14), anchor.AddDays(35)),
            6 => (anchor.AddDays(30), null),
            _ => (null, null),
        };
    }
}
