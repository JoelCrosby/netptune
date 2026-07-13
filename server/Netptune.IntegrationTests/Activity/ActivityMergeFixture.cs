using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Activity.Handlers;
using Netptune.Activity.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Configuration;

using Testcontainers.PostgreSql;

using Xunit;


namespace Netptune.IntegrationTests.Activity;

// A dedicated Postgres: these tests write, sweep and delete activity rows that the endpoint tests read.
public sealed class ActivityMergeFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceProvider Provider = null!;

    public RecordingNotificationEventPublisher NotificationEvents { get; } = new();

    public IServiceScopeFactory ScopeFactory => Provider.GetRequiredService<IServiceScopeFactory>();

    public string ActorUserId { get; private set; } = null!;

    public string OtherUserId { get; private set; } = null!;

    public string ThirdUserId { get; private set; } = null!;

    public int WorkspaceId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var connectionString = DbContainer.GetConnectionString();

        var services = new ServiceCollection();

        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddNetptuneEntities(options => options.ConnectionString = connectionString);
        services.AddNetptuneRepository(options => options.ConnectionString = connectionString);
        services.AddSingleton<INotificationEventPublisher>(NotificationEvents);

        Provider = services.BuildServiceProvider();

        using var scope = Provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.Database.EnsureCreatedAsync();

        var workspace = new Workspace
        {
            Name = "Merge",
            Slug = "merge",
            CreatedAt = DateTime.UtcNow,
            MetaInfo = new (),
        };

        db.Workspaces.Add(workspace);

        var users = new[] { "actor", "other", "third" }
            .Select(name => new AppUser
            {
                UserName = $"{name}@netptune.co.uk",
                Email = $"{name}@netptune.co.uk",
                Firstname = name,
                Lastname = "Merge",
            })
            .ToList();

        db.Users.AddRange(users);

        await db.SaveChangesAsync();

        db.WorkspaceAppUsers.AddRange(users.Select(user => new WorkspaceAppUser
        {
            WorkspaceId = workspace.Id,
            UserId = user.Id,
            Role = WorkspaceRole.Member,
            Permissions = [],
        }));

        await db.SaveChangesAsync();

        WorkspaceId = workspace.Id;
        ActorUserId = users[0].Id;
        OtherUserId = users[1].Id;
        ThirdUserId = users[2].Id;
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    // Its own scope, DataContext and connection — two of these are two replicas as far as Postgres is
    // concerned, which is what the concurrency tests need.
    public (IServiceScope Scope, ActivityHandler Handler) CreateHandler(ActivityMergeOptions? merge = null)
    {
        var scope = Provider.CreateScope();

        var handler = new ActivityHandler(
            scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>(),
            scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>(),
            Options.Create(merge ?? new ActivityMergeOptions()));

        return (scope, handler);
    }

    public ActivityMergeWindowJob CreateSweeper(ActivityMergeOptions? merge = null)
    {
        return new (
            ScopeFactory,
            Options.Create(merge ?? new ActivityMergeOptions()),
            Provider.GetRequiredService<ILogger<ActivityMergeWindowJob>>());
    }


    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await DbContainer.DisposeAsync();
    }
}

public sealed class RecordingNotificationEventPublisher : INotificationEventPublisher
{
    public ConcurrentBag<UserNotificationEvent> Published { get; } = [];

    public Task PublishAsync(string userId, NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        Published.Add(new (userId, notificationEvent));

        return Task.CompletedTask;
    }

    public Task PublishManyAsync(IEnumerable<UserNotificationEvent> notificationEvents, CancellationToken cancellationToken = default)
    {
        foreach (var notificationEvent in notificationEvents)
        {
            Published.Add(notificationEvent);
        }

        return Task.CompletedTask;
    }
}
