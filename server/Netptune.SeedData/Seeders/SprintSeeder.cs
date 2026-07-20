using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class SprintSeeder : ISeeder
{
    private const int CompletedSprintTaskCount = 6;
    private const int ActiveSprintTaskCount = 8;

    private sealed record SprintSeed(string Name, string Goal);

    private static readonly Dictionary<string, (SprintSeed Completed, SprintSeed Active)> ProjectSprints = new()
    {
        ["PAT"] = (
            new SprintSeed(
                "Identity and Access Hardening",
                "Close the highest-risk authentication and authorization gaps across the public API."),
            new SprintSeed(
                "Resilient API Delivery",
                "Improve API reliability under retries, traffic spikes, and third-party service failures.")),
        ["MOB"] = (
            new SprintSeed(
                "Mobile Stability Release",
                "Resolve the most disruptive iOS and Android crashes and strengthen release confidence."),
            new SprintSeed(
                "Offline-First Experience",
                "Make core task workflows reliable on slow or intermittent mobile connections.")),
        ["DSH"] = (
            new SprintSeed(
                "Trusted Metrics Foundation",
                "Standardise product metrics and remove inconsistencies from the analytics pipeline."),
            new SprintSeed(
                "Executive Reporting",
                "Deliver decision-ready dashboards for product, revenue, and customer health.")),
        ["MKT"] = (
            new SprintSeed(
                "Conversion Foundations",
                "Improve landing-page performance, accessibility, and the reliability of conversion tracking."),
            new SprintSeed(
                "Organic Growth Launch",
                "Publish the next search-led content release and strengthen technical SEO coverage.")),
        ["CLI"] = (
            new SprintSeed(
                "Workspace Automation",
                "Automate common project setup and workspace maintenance tasks in the CLI."),
            new SprintSeed(
                "Developer Workflow Polish",
                "Reduce friction in everyday commands with clearer output, safer defaults, and faster execution.")),
        ["CMP"] = (
            new SprintSeed(
                "Accessible Foundations",
                "Bring core interaction components in line with keyboard and screen-reader expectations."),
            new SprintSeed(
                "Design System Adoption",
                "Expand documented component coverage and make adoption easier across product teams.")),
        ["INF"] = (
            new SprintSeed(
                "Cluster Reliability",
                "Remove the largest availability risks from the Kubernetes and cloud platform."),
            new SprintSeed(
                "Safer Delivery Pipelines",
                "Improve deployment confidence with stronger validation, rollback, and environment controls.")),
        ["MON"] = (
            new SprintSeed(
                "Actionable Alerting",
                "Reduce noisy alerts and make every page carry enough context for an effective response."),
            new SprintSeed(
                "End-to-End Observability",
                "Close tracing and dashboard gaps across user requests, background work, and infrastructure.")),
    };

    public int Phase => 2;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var activeStart = StartOfWeek(DateTime.UtcNow.Date);
        var activeEnd = activeStart.AddDays(13);
        var completedStart = activeStart.AddDays(-14);
        var completedEnd = activeStart.AddDays(-1);

        foreach (var (project, projectIndex) in context.Projects.Select((project, index) => (project, index)))
        {
            var plan = ProjectSprints[project.Key];
            var workspaceUsers = context.UsersFor(project.Workspace);
            var completedSprint = new Sprint
            {
                Name = plan.Completed.Name,
                Goal = plan.Completed.Goal,
                Status = SprintStatus.Completed,
                StartDate = completedStart,
                StartedAt = completedStart,
                EndDate = completedEnd,
                CompletedAt = completedEnd,
                Project = project,
                Workspace = project.Workspace,
                Owner = workspaceUsers[projectIndex % workspaceUsers.Count],
            };
            var activeSprint = new Sprint
            {
                Name = plan.Active.Name,
                Goal = plan.Active.Goal,
                Status = SprintStatus.Active,
                StartDate = activeStart,
                StartedAt = activeStart,
                EndDate = activeEnd,
                Project = project,
                Workspace = project.Workspace,
                Owner = workspaceUsers[(projectIndex + 1) % workspaceUsers.Count],
            };

            context.Sprints.Add(completedSprint);
            context.Sprints.Add(activeSprint);

            AssignProjectTasks(context, project, completedSprint, activeSprint);
        }

        EnsureSprintNamesAreUnique(context.Sprints);

        await dbContext.Sprints.AddRangeAsync(context.Sprints, ct);
    }

    private static void AssignProjectTasks(
        SeedContext context,
        Project project,
        Sprint completedSprint,
        Sprint activeSprint)
    {
        var projectTasks = context.Tasks.Where(task => task.Project == project).ToList();
        var completedStatus = context.Statuses.First(status =>
            status.Workspace == project.Workspace &&
            status.EntityType == EntityType.Task &&
            status.Category == StatusCategory.Done);
        var completedStart = DateOnly.FromDateTime(completedSprint.StartDate);
        var completedEnd = DateOnly.FromDateTime(completedSprint.EndDate);
        var activeStart = DateOnly.FromDateTime(activeSprint.StartDate);
        var activeEnd = DateOnly.FromDateTime(activeSprint.EndDate);

        foreach (var (task, taskIndex) in projectTasks
                     .Take(CompletedSprintTaskCount)
                     .Select((task, index) => (task, index)))
        {
            var taskStart = completedStart.AddDays(taskIndex * 2);
            var proposedDueDate = taskStart.AddDays(3);

            task.Sprint = completedSprint;
            task.Status = completedStatus;
            task.StartDate = taskStart;
            task.DueDate = proposedDueDate > completedEnd ? completedEnd : proposedDueDate;
        }

        foreach (var (task, taskIndex) in projectTasks
                     .Skip(CompletedSprintTaskCount)
                     .Take(ActiveSprintTaskCount)
                     .Select((task, index) => (task, index)))
        {
            var taskStart = activeStart.AddDays(taskIndex);
            var proposedDueDate = taskStart.AddDays(4);

            task.Sprint = activeSprint;
            task.StartDate = taskStart;
            task.DueDate = proposedDueDate > activeEnd ? activeEnd : proposedDueDate;
        }
    }

    private static void EnsureSprintNamesAreUnique(IReadOnlyCollection<Sprint> sprints)
    {
        var duplicate = sprints
            .GroupBy(sprint => $"{sprint.Workspace.Slug}:{sprint.Name}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Sprint name '{duplicate.Key}' is duplicated in seed data");
        }
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;

        return date.AddDays(-daysSinceMonday);
    }
}
