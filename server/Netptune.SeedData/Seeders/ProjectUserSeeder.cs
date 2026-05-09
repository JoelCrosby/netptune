using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class ProjectUserSeeder : ISeeder
{
    public int Phase => 1;

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.ProjectUsers.AddRange(
            context.Projects.SelectMany(project => context.Users.Select(user => new ProjectUser
            {
                Project = project,
                User = user,
            }))
        );

        await dbContext.ProjectUsers.AddRangeAsync(context.ProjectUsers, ct);
    }
}
