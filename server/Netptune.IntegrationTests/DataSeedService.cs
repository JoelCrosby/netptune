using System.Diagnostics;

using Bogus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.IntegrationTests;

internal sealed class DataSeedService : IHostedService
{
    private readonly IServiceProvider ServiceProvider;

    private readonly List<string> Workspaces = new () { "Netptune", "Linux" };
    private readonly List<string> Projects = new () { "NeoVim", "VsCode", "Emacs", "Kakoune" };
    private readonly List<string> Tags = new ()
    {
        "Typescript",
        "Python",
        "Go",
        "Java",
        "Kotlin",
        "C#",
        "Swift",
    };

    public static readonly List<string> UserIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToList();

    public DataSeedService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Randomizer.Seed = new (1_000_001);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();

        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var users = new Faker<AppUser>()
            .RuleFor(p => p.Id, f => UserIds.ElementAt(f.IndexFaker))
            .RuleFor(p => p.Firstname, f => f.Person.FirstName)
            .RuleFor(p => p.Lastname, f => f.Person.LastName)
            .RuleFor(p => p.Email, f => f.Person.Email)
            .RuleFor(p => p.PictureUrl, f => f.Person.Avatar)
            .RuleFor(p => p.UserName, (_, u) => u.Email)
            .RuleFor(p => p.NormalizedEmail, (_, u) => u.Email!.Normalize().ToUpperInvariant())
            .RuleFor(p => p.NormalizedUserName, (_, u) => u.UserName!.Normalize().ToUpperInvariant())
            .Generate(3);

        var workspaces = new Faker<Workspace>()
            .RuleFor(w => w.Name, f => Workspaces.ElementAt(f.IndexFaker))
            .RuleFor(w => w.Description, f => f.Company.Bs())
            .RuleFor(w => w.Slug, (_, w) => w.Name.ToUrlSlug())
            .RuleFor(w => w.MetaInfo, f => new () { Color = f.Internet.Color() })
            .RuleFor(p => p.Owner, f => f.PickRandom(users))
            .Generate(Workspaces.Count);

        var workspaceUsers = new Faker<WorkspaceAppUser>()
            .RuleFor(p => p.User, f => users.ElementAt(f.IndexFaker))
            .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
            .Generate(users.Count);

        var projects = new Faker<Project>()
            .RuleFor(p => p.Name, f => Projects.ElementAt(f.IndexFaker))
            .RuleFor(p => p.Description, f => f.Company.Bs())
            .RuleFor(p => p.Key, (_, p) => p.Name.ToUrlSlug().Substring(0, 3))
            .RuleFor(p => p.MetaInfo, _ => new())
            .RuleFor(p => p.Owner, f => f.PickRandom(users))
            .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
            .Generate(4);

        var boards = new Faker<Board>()
            .RuleFor(p => p.Name, f => Projects.ElementAt(f.IndexFaker))
            .RuleFor(p => p.Identifier, (_, p) => p.Name.ToUrlSlug())
            .RuleFor(p => p.BoardType, _ => BoardType.Default)
            .RuleFor(p => p.MetaInfo, _ => new())
            .RuleFor(p => p.Owner, f => f.PickRandom(users))
            .RuleFor(p => p.Project, f => projects.ElementAt(f.IndexFaker))
            .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
            .Generate(4);

        var boardGroups = new []
        {
            new Faker<BoardGroup>()
                .RuleFor(p => p.Name, f => Projects.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Owner, f => f.PickRandom(users))
                .RuleFor(p => p.Board, f => boards.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
                .RuleFor(p => p.Type, _ => BoardGroupType.Backlog)
                .Generate(4),
            new Faker<BoardGroup>()
                .RuleFor(p => p.Name, f => Projects.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Owner, f => f.PickRandom(users))
                .RuleFor(p => p.Board, f => boards.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
                .RuleFor(p => p.Type, _ => BoardGroupType.Todo)
                .Generate(4),
            new Faker<BoardGroup>()
                .RuleFor(p => p.Name, f => Projects.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Owner, f => f.PickRandom(users))
                .RuleFor(p => p.Board, f => boards.ElementAt(f.IndexFaker))
                .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
                .RuleFor(p => p.Type, _ => BoardGroupType.Done)
                .Generate(4),
        }
            .SelectMany(list => list)
            .ToList();

        var tasks = new Faker<ProjectTask>()
            .RuleFor(p => p.Name, f => f.System.Exception().Message)
            .RuleFor(p => p.Description, f => f.System.Exception().StackTrace)
            .RuleFor(p => p.Status, f => f.PickRandom<ProjectTaskStatus>())
            .RuleFor(p => p.IsFlagged, f => f.Random.Bool())
            .RuleFor(p => p.Owner, f => f.PickRandom(users))
            .RuleFor(p => p.Project, f => f.PickRandom(projects))
            .RuleFor(p => p.ProjectScopeId, f => f.IndexFaker)
            .RuleFor(p => p.Workspace, (_, p) => p.Project!.Workspace)
            .Generate(4);

        try
        {
            await context.Database.EnsureCreatedAsync(ct);
            await context.Database.BeginTransactionAsync(ct);

            await context.Users.AddRangeAsync(users, ct);
            await context.Workspaces.AddRangeAsync(workspaces, ct);
            await context.WorkspaceAppUsers.AddRangeAsync(workspaceUsers, ct);
            await context.Boards.AddRangeAsync(boards, ct);
            await context.BoardGroups.AddRangeAsync(boardGroups, ct);
            await context.Projects.AddRangeAsync(projects, ct);
            await context.ProjectTasks.AddRangeAsync(tasks, ct);

            await context.SaveChangesAsync(ct);

            var activityLogs = new Faker<ActivityLog>()
                .RuleFor(p => p.EntityType, _ => EntityType.Task)
                .RuleFor(p => p.TaskId, f => f.PickRandom(tasks).Id)
                .RuleFor(p => p.EntityId, (_, p) => p.TaskId)
                .RuleFor(p => p.Time, f => f.Date.Recent())
                .RuleFor(p => p.Type, f => f.PickRandom<ActivityType>())
                .RuleFor(p => p.User, f => f.PickRandom(users))
                .RuleFor(p => p.Workspace, f => f.PickRandom(workspaces))
                .Generate(32);

            var comments = new Faker<Comment>()
                .RuleFor(p => p.Body, f => f.System.Exception().Message)
                .RuleFor(p => p.EntityId, f => f.PickRandom(tasks).Id)
                .RuleFor(p => p.EntityType, EntityType.Task)
                .RuleFor(p => p.Owner, f => f.PickRandom(users))
                .RuleFor(p => p.WorkspaceId, f => f.PickRandom(tasks).WorkspaceId)
                .Generate(32);

            var tags = new Faker<Tag>()
                .RuleFor(p => p.Owner, f => f.PickRandom(users))
                .RuleFor(p => p.WorkspaceId, f => f.PickRandom(tasks).WorkspaceId)
                .RuleFor(p => p.Name, (f, p) => f.PickRandom(Tags) + f.IndexFaker)
                .Generate(32);

            var taskTags = new Faker<ProjectTaskTag>()
                .RuleFor(p => p.Tag, f => f.PickRandom(tags))
                .RuleFor(p => p.ProjectTask, f => f.PickRandom(tasks))
                .Generate(users.Count);

            await context.ActivityLogs.AddRangeAsync(activityLogs, ct);
            await context.Comments.AddRangeAsync(comments, ct);
            await context.Tags.AddRangeAsync(tags, ct);
            await context.ProjectTaskTags.AddRangeAsync(taskTags, ct);

            await context.SaveChangesAsync(ct);
            await context.Database.CommitTransactionAsync(ct);
        }
        catch
        {
            await context.Database.RollbackTransactionAsync(ct);
            throw;
        }

        Console.WriteLine($"timer elapsed {timer.Elapsed:'hh:ss'}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
