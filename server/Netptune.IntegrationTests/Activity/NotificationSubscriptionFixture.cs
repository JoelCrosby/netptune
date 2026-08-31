using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Activity.Handlers;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
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

// A dedicated Postgres, like the merge fixture: these tests write and read notification rows for a
// fixed board and sprint, and the endpoint tests read the same tables.
public sealed class NotificationSubscriptionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceProvider Provider = null!;

    public RecordingNotificationEventPublisher NotificationEvents { get; } = new();

    public string ActorUserId { get; private set; } = null!;

    public string SubscriberUserId { get; private set; } = null!;

    public int WorkspaceId { get; private set; }

    public int ProjectId { get; private set; }

    public int BoardId { get; private set; }

    public int OtherBoardId { get; private set; }

    public int BoardGroupId { get; private set; }

    public int OtherBoardGroupId { get; private set; }

    public int SprintId { get; private set; }

    public int TaskId { get; private set; }

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
        await Seed(db);
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public (IServiceScope Scope, ActivityHandler Handler) CreateHandler()
    {
        var scope = Provider.CreateScope();

        var handler = new ActivityHandler(
            scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>(),
            scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>(),
            Options.Create(new ActivityMergeOptions()),
            scope.ServiceProvider.GetRequiredService<DataContext>());

        return (scope, handler);
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await DbContainer.DisposeAsync();
    }

    private async Task Seed(DataContext db)
    {
        var workspace = new Workspace
        {
            Name = "Subscriptions",
            Slug = "subscriptions",
            CreatedAt = DateTime.UtcNow,
            MetaInfo = new(),
        };

        db.Workspaces.Add(workspace);

        var users = new[] { "actor", "subscriber" }
            .Select(name => new AppUser
            {
                UserName = $"{name}@subscriptions.netptune.co.uk",
                Email = $"{name}@subscriptions.netptune.co.uk",
                Firstname = name,
                Lastname = "Subscriptions",
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

        var status = new Status
        {
            Name = "Todo",
            Key = "todo",
            EntityType = EntityType.Task,
            Category = StatusCategory.Todo,
            WorkspaceId = workspace.Id,
        };

        db.Statuses.Add(status);

        var project = new Project
        {
            Name = "Subscriptions",
            Key = "SUB",
            WorkspaceId = workspace.Id,
            MetaInfo = new ProjectMeta(),
        };

        db.Projects.Add(project);

        await db.SaveChangesAsync();

        var board = NewBoard("Board", "board-1", project.Id, workspace.Id);
        var otherBoard = NewBoard("Other Board", "board-2", project.Id, workspace.Id);

        db.Boards.AddRange(board, otherBoard);

        var sprint = new Sprint
        {
            Name = "Sprint One",
            Status = SprintStatus.Active,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(14),
            ProjectId = project.Id,
            WorkspaceId = workspace.Id,
        };

        db.Sprints.Add(sprint);

        await db.SaveChangesAsync();

        var boardGroup = new BoardGroup
        {
            Name = "Backlog",
            BoardId = board.Id,
            SortOrder = 1,
            WorkspaceId = workspace.Id,
        };

        var otherBoardGroup = new BoardGroup
        {
            Name = "Doing",
            BoardId = board.Id,
            SortOrder = 2,
            WorkspaceId = workspace.Id,
        };

        db.BoardGroups.AddRange(boardGroup, otherBoardGroup);

        var task = new ProjectTask
        {
            Name = "Subscribed task",
            StatusId = status.Id,
            ProjectScopeId = 1,
            ProjectId = project.Id,
            SprintId = sprint.Id,
            WorkspaceId = workspace.Id,
        };

        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();

        db.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            ProjectTaskId = task.Id,
            BoardGroupId = boardGroup.Id,
            SortOrder = 1,
        });

        await db.SaveChangesAsync();

        WorkspaceId = workspace.Id;
        ActorUserId = users[0].Id;
        SubscriberUserId = users[1].Id;
        ProjectId = project.Id;
        BoardId = board.Id;
        OtherBoardId = otherBoard.Id;
        BoardGroupId = boardGroup.Id;
        OtherBoardGroupId = otherBoardGroup.Id;
        SprintId = sprint.Id;
        TaskId = task.Id;
    }

    private static Board NewBoard(string name, string identifier, int projectId, int workspaceId)
    {
        return new Board
        {
            Name = name,
            Identifier = identifier,
            ProjectId = projectId,
            BoardType = BoardType.UserDefined,
            WorkspaceId = workspaceId,
            MetaInfo = new BoardMeta(),
        };
    }
}
