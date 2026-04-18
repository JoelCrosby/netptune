using Netptune.Core.Entities;
using Netptune.Core.Relationships;

namespace Netptune.TestData.Seeders;

internal static class TaskTagSeeder
{
    internal static List<ProjectTaskTag> Generate(List<Tag> tags, List<ProjectTask> tasks, List<AppUser> users) =>
        users.Select((_, i) => new ProjectTaskTag
        {
            Tag = tags[i % tags.Count],
            ProjectTask = tasks[i % tasks.Count],
        }).ToList();
}
