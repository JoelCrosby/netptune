using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Entities;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Repositories;
using Netptune.Repositories.ConnectionFactories;

using Testcontainers.PostgreSql;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public sealed class ActivityFeedFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceProvider Provider = null!;

    private string ConnectionString => DbContainer.GetConnectionString();

    public string UserId { get; private set; } = null!;

    public string OtherUserId { get; private set; } = null!;

    public int WorkspaceId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddNetptuneEntities(options => options.ConnectionString = ConnectionString);

        Provider = services.BuildServiceProvider();

        await new HostedDatabaseService(Provider.GetRequiredService<IServiceScopeFactory>())
            .StartAsync(CancellationToken.None);

        using var scope = Provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var user = new AppUser
        {
            UserName = "feed@netptune.co.uk",
            Email = "feed@netptune.co.uk",
            Firstname = "Feed",
            Lastname = "Reader",
        };

        var other = new AppUser
        {
            UserName = "other@netptune.co.uk",
            Email = "other@netptune.co.uk",
            Firstname = "Other",
            Lastname = "Actor",
        };

        var workspace = new Workspace
        {
            Name = "Feed",
            Slug = "feed",
            CreatedAt = DateTime.UtcNow,
            MetaInfo = new(),
        };

        db.Users.AddRange(user, other);
        db.Workspaces.Add(workspace);

        await db.SaveChangesAsync();

        UserId = user.Id;
        OtherUserId = other.Id;
        WorkspaceId = workspace.Id;
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public EventRecordRepository CreateRepository(DataContext db)
    {
        return new EventRecordRepository(db, new NetptuneConnectionFactory(ConnectionString));
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await DbContainer.DisposeAsync();
    }
}
