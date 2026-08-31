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
        """
        Current implementation stores tokens in `localStorage`. Switch to httpOnly cookies with refresh token rotation to meet security compliance requirements.

        ## Acceptance

        - [ ] Access tokens live in an httpOnly, SameSite=Strict cookie
        - [ ] Refresh tokens rotate on every use and the one they replace is revoked
        - [ ] Reusing a revoked token invalidates the whole session family
        """,
        """
        Instrument all controllers and service boundaries with activity spans. Export traces to the collector endpoint configured in app settings.

        ## Scope

        1. One `ActivitySource` per assembly, named after the assembly
        2. Spans around every mediator handler and every outbound http call
        3. Baggage carries the workspace key so traces can be filtered per tenant

        Sampling stays at 100% until the collector is sized — see [the collector docs](https://opentelemetry.io/docs/collector/).
        """,
        """
        The component uses a mix of `BehaviorSubject`s and local state. Consolidate into a feature store with selectors for all derived state.

        > The store is the only writer. Components read signals and dispatch, they *never* mutate.

        - Move the filter and sort state into the store
        - Derive the visible rows with a computed selector
        - Drop the manual teardown once nothing subscribes by hand
        """,
        """
        API endpoints are creating new `DbContext` instances per request without pooling. Configure `AddDbContextPool` with an appropriate pool size.

        ```csharp
        builder.Services.AddDbContextPool<DataContext>(
            options => options.UseNpgsql(connectionString),
            poolSize: 256);
        ```

        Measure either side of the change with the load profile in the performance runbook.
        """,
        """
        The checkout workflow has no automated coverage. Add Playwright e2e tests for the happy path, payment failure, and session timeout scenarios.

        - [x] Test project scaffolded and running in CI
        - [ ] Happy path: cart, address, payment, confirmation
        - [ ] A declined payment leaves the cart intact
        - [ ] A timed out session returns to sign in and restores the cart
        """,
        """
        Several feature modules duplicate button, input, and modal components. Extract into a shared library with a consistent design token system.

        ### Candidates

        - **Button** — four variants across three modules today
        - **Input** — two implementations, only one handles the disabled state
        - **Modal** — the focus trap differs between the copies

        ---

        Naming follows the `--color-*` variables already in the stylesheet; no new tokens are introduced.
        """,
        """
        The API deployment scales manually. Define HPA rules based on CPU and request queue depth metrics from the Prometheus scrape endpoint.

        ```yaml
        minReplicas: 2
        maxReplicas: 12
        targetCPUUtilizationPercentage: 70
        ```

        Queue depth needs the **custom metrics adapter** in place before it can be a target.
        """,
        """
        EF Core 9 introduces breaking changes to the query pipeline and owned entity mapping. Resolve migration conflicts and update affected raw SQL queries.

        1. Bump the package versions and build against the new analyzers
        2. Rewrite the owned entity mappings that relied on the old table splitting defaults
        3. Re-check every query under `Sql/` against the plan it now produces
        """,
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
