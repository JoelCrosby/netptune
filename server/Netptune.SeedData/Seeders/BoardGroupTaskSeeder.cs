using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class BoardGroupTaskSeeder : ISeeder
{
    public int Phase => 2;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var entries = new List<ProjectTaskInBoardGroup>();

        foreach (var project in context.Projects)
        {
            var tasks = context.Tasks.Where(t => t.Project == project).ToList();
            var board = context.Boards.First(b => b.Project == project);
            var groups = context.BoardGroups.Where(g => g.Board == board).ToList();

            for (var i = 0; i < tasks.Count; i++)
            {
                entries.Add(new ProjectTaskInBoardGroup
                {
                    ProjectTask = tasks[i],
                    BoardGroup = groups[i % groups.Count],
                    // ReSharper disable once PossibleLossOfFraction
                    SortOrder = i / groups.Count,
                });
            }
        }

        await dbContext.ProjectTaskInBoardGroups.AddRangeAsync(entries, ct);
    }
}
