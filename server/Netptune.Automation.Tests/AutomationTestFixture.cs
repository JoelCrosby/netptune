using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;
using Netptune.Entities.Interceptors;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Repositories.UnitOfWork;

using Npgsql;

using Testcontainers.PostgreSql;

using Xunit;

[assembly: AssemblyFixture(typeof(Netptune.Automation.Tests.AutomationTestFixture))]

namespace Netptune.Automation.Tests;

public sealed class AutomationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceProvider Services = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.MapEnum<WorkspaceRole>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<SprintStatus>();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<DataContext>(options =>
        {
            options
                .UseNpgsql(DbContainer.GetConnectionString(), npgsql =>
                {
                    npgsql.MapEnum<WorkspaceRole>();
                    npgsql.MapEnum<SprintStatus>();
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(new AuditLogImmutabilityInterceptor());
        });

        services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(DbContainer.GetConnectionString()));
        services.AddScoped<INetptuneUnitOfWork, NetptuneUnitOfWork>();
        services.AddSingleton<INotificationEventPublisher, NoOpNotificationEventPublisher>();
        services.AddNetptuneAutomation();

        Services = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await DbContainer.DisposeAsync();
    }

    public async Task<AutomationTestScope> CreateScope()
    {
        var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await ResetDatabase(db);

        return new AutomationTestScope(scope);
    }

    private static async Task ResetDatabase(DataContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                activity_logs,
                automation_actions,
                automation_runs,
                automation_rules,
                flags,
                notifications,
                project_task_app_users,
                project_tasks,
                projects,
                workspace_app_users,
                workspaces,
                users
            RESTART IDENTITY CASCADE;
            """);

        db.ChangeTracker.Clear();
    }

    private sealed class NoOpNotificationEventPublisher : INotificationEventPublisher
    {
        public Task PublishAsync(
            string userId,
            NotificationEvent notificationEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishManyAsync(
            IEnumerable<UserNotificationEvent> notificationEvents,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
