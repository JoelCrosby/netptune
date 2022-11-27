﻿using Bogus;

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

    public DataSeedService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Randomizer.Seed = new (1_000_001);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var users = new Faker<AppUser>()
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
            await context.Projects.AddRangeAsync(projects, ct);
            await context.ProjectTasks.AddRangeAsync(tasks, ct);

            await context.SaveChangesAsync(ct);
            await context.Database.CommitTransactionAsync(ct);
        }
        catch
        {
            await context.Database.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}