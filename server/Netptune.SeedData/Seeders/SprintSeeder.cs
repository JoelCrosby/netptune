using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class SprintSeeder : ISeeder
{
    public int Phase => 1;

    private static readonly DateTime Sprint1Start = new(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Sprint1End   = new(2025, 1, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Sprint2Start = new(2025, 1, 27, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Sprint2End   = new(2025, 2, 14, 0, 0, 0, DateTimeKind.Utc);

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Sprints.AddRange(context.Projects.SelectMany((project, pi) => new Sprint[]
        {
            new()
            {
                Name = "Sprint 1",
                Goal = "Establish the foundational feature set and address the highest-priority backlog items.",
                Status = SprintStatus.Completed,
                StartDate = Sprint1Start,
                EndDate = Sprint1End,
                CompletedAt = Sprint1End,
                Project = project,
                Workspace = project.Workspace,
                Owner = context.Users[pi % context.Users.Count],
            },
            new()
            {
                Name = "Sprint 2",
                Goal = "Deliver the next set of planned features and resolve any issues surfaced in Sprint 1.",
                Status = SprintStatus.Active,
                StartDate = Sprint2Start,
                EndDate = Sprint2End,
                Project = project,
                Workspace = project.Workspace,
                Owner = context.Users[(pi + 1) % context.Users.Count],
            },
        }));

        await dbContext.Sprints.AddRangeAsync(context.Sprints, ct);
    }
}
