using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SearchIndexSourceTests
{
    private const string WorkspaceSlug = "linux";

    private readonly NetptuneFixture Fixture;

    public SearchIndexSourceTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task GetAllTaskViewModels_ShouldReturnEveryTask_WhenWorkspaceExceedsAPage()
    {
        var ct = TestContext.Current.CancellationToken;

        await SeedTasks(140, ct);

        using var scope = Fixture.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

        var pagedDefault = await unitOfWork.Tasks.GetTasksAsync(WorkspaceSlug, cancellationToken: ct);
        var all = await unitOfWork.Tasks.GetAllTaskViewModels(WorkspaceSlug, ct);

        pagedDefault.Items.Should().HaveCount(PaginationDefaults.DefaultPageSize);
        all.Should().HaveCount(pagedDefault.TotalCount);
        all.Count.Should().BeGreaterThan(PaginationDefaults.MaxPageSize);
        all.Select(task => task.Id).Should().OnlyHaveUniqueItems();
    }

    private async Task SeedTasks(int count, CancellationToken ct)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var workspace = await context.Workspaces.SingleAsync(item => item.Slug == WorkspaceSlug, ct);
        var project = await context.Projects.FirstAsync(item => item.WorkspaceId == workspace.Id, ct);
        var status = await context.Statuses.FirstAsync(
            item => item.WorkspaceId == workspace.Id && item.EntityType == EntityType.Task, ct);

        var existing = await context.ProjectTasks.CountAsync(
            task => task.WorkspaceId == workspace.Id && !task.IsDeleted, ct);

        if (existing >= count) return;

        var nextScopeId = await context.ProjectTasks
            .Where(task => task.ProjectId == project.Id)
            .MaxAsync(task => (int?)task.ProjectScopeId, ct) ?? 0;

        var tasks = Enumerable.Range(1, count - existing).Select(offset => new ProjectTask
        {
            Name = $"Paging fixture task {offset}",
            ProjectScopeId = nextScopeId + offset,
            ProjectId = project.Id,
            WorkspaceId = workspace.Id,
            StatusId = status.Id,
            OwnerId = project.OwnerId,
        });

        await context.ProjectTasks.AddRangeAsync(tasks, ct);
        await context.SaveChangesAsync(ct);
    }
}
