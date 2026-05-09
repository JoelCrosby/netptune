using Netptune.Entities.Contexts;

namespace Netptune.SeedData;

public interface ISeeder
{
    int Phase { get; }

    Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct);
}
