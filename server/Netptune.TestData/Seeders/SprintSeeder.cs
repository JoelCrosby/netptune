using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class SprintSeeder
{
    private static readonly DateTime FirstStart = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

    private static readonly SprintStatus[] Statuses =
    [
        SprintStatus.Completed,
        SprintStatus.Active,
        SprintStatus.Planning,
        SprintStatus.Planning,
    ];

    internal static List<Sprint> Generate(List<Project> projects)
    {
        return projects.Select((project, i) =>
        {
            var start = FirstStart.AddDays(i * 14);
            var status = Statuses[i % Statuses.Length];

            return new Sprint
            {
                Name = $"{project.Name} Sprint 1",
                Goal = $"Ship the first slice of {project.Name}",
                Status = status,
                StartDate = start,
                EndDate = start.AddDays(13),
                StartedAt = status is SprintStatus.Planning ? null : start,
                CompletedAt = status is SprintStatus.Completed ? start.AddDays(13) : null,
                Project = project,
                Workspace = project.Workspace,
            };
        }).ToList();
    }
}
