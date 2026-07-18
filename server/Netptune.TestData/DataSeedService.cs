using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Netptune.Entities.Contexts;
using Netptune.TestData.Seeders;

namespace Netptune.TestData;

public sealed class DataSeedService : IHostedService
{
    private readonly IServiceProvider ServiceProvider;
    private readonly ILogger<DataSeedService> Logger;

    public DataSeedService(IServiceProvider serviceProvider, ILogger<DataSeedService> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        Logger.LogInformation("{Service} starting data seed execution", nameof(DataSeedService));

        var timer = Stopwatch.StartNew();

        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var users = UserSeeder.Generate();
        var workspaces = WorkspaceSeeder.Generate(users);
        var workspaceUsers = WorkspaceUserSeeder.Generate(workspaces, users);
        var statuses = StatusSeeder.Generate(workspaces);
        var relationTypes = RelationTypeSeeder.Generate(workspaces);
        var projects = ProjectSeeder.Generate(users, workspaces, statuses);
        var boards = BoardSeeder.Generate(users, projects);
        var boardGroups = BoardGroupSeeder.Generate(users, boards);
        var tasks = TaskSeeder.Generate(users, projects, statuses);

        try
        {
            await context.Database.EnsureCreatedAsync(ct);
            await context.Database.BeginTransactionAsync(ct);

            await context.Users.AddRangeAsync(users, ct);
            await context.Workspaces.AddRangeAsync(workspaces, ct);
            await context.WorkspaceAppUsers.AddRangeAsync(workspaceUsers, ct);
            await context.Statuses.AddRangeAsync(statuses, ct);
            await context.RelationTypes.AddRangeAsync(relationTypes, ct);
            await context.Boards.AddRangeAsync(boards, ct);
            await context.BoardGroups.AddRangeAsync(boardGroups, ct);
            await context.Projects.AddRangeAsync(projects, ct);
            await context.ProjectTasks.AddRangeAsync(tasks, ct);

            await context.SaveChangesAsync(ct);

            var activityLogs = EventRecordSeeder.Generate(tasks, users, workspaces);
            var comments = CommentSeeder.Generate(tasks, users);
            var tags = TagSeeder.Generate(users, tasks);
            var taskTags = TaskTagSeeder.Generate(tags, tasks, users);

            await context.EventRecords.AddRangeAsync(activityLogs, ct);
            await context.Comments.AddRangeAsync(comments, ct);
            await context.Tags.AddRangeAsync(tags, ct);
            await context.ProjectTaskTags.AddRangeAsync(taskTags, ct);

            await context.SaveChangesAsync(ct);

            var notifications = NotificationSeeder.Generate(activityLogs, workspaces, workspaceUsers);
            var activityEntries = ActivityEntrySeeder.Generate(activityLogs, users, workspaces);

            await context.Notifications.AddRangeAsync(notifications, ct);
            await context.ActivityEntries.AddRangeAsync(activityEntries, ct);

            await context.SaveChangesAsync(ct);
            await context.Database.CommitTransactionAsync(ct);
        }
        catch
        {
            await context.Database.RollbackTransactionAsync(ct);
            throw;
        }

        timer.Stop();

        Logger.LogInformation("{Service} finished execution in {Elapsed}", nameof(DataSeedService), $"{timer.ElapsedMilliseconds:N}ms");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
