using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class TaskTagSeeder : ISeeder
{
    public int Phase => 2;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.TaskTags.AddRange(
            context.Tasks.Select((task, i) =>
            {
                var workspaceTags = context.Tags.Where(t => t.Workspace == task.Workspace).ToList();

                return new ProjectTaskTag
                {
                    ProjectTask = task,
                    Tag = workspaceTags[i % workspaceTags.Count],
                };
            })
        );

        await dbContext.ProjectTaskTags.AddRangeAsync(context.TaskTags, ct);
    }
}
