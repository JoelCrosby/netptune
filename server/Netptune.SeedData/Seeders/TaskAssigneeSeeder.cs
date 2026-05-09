using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class TaskAssigneeSeeder : ISeeder
{
    public int Phase => 2;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        var assignees = context.Tasks.SelectMany((task, i) =>
        {
            var workspaceUsers = context.UsersFor(task.Workspace);
            var count = (i % 2) + 1;

            return Enumerable.Range(0, count).Select(j => new ProjectTaskAppUser
            {
                ProjectTask = task,
                UserId = workspaceUsers[(i + j) % workspaceUsers.Count].Id,
            });
        }).ToList();

        context.TaskAssignees.AddRange(assignees);

        await dbContext.ProjectTaskAppUsers.AddRangeAsync(assignees, ct);
    }
}
