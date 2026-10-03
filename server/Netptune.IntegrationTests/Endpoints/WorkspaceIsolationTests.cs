using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// Every request runs in the netptune workspace and names an entity that belongs to linux. Each one
// used to reach across, because the handler looked the entity up by its raw id alone.
public sealed class WorkspaceIsolationTests
{
    private const string ForeignWorkspace = "linux";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public WorkspaceIsolationTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetTask_ShouldReturnNotFound_WhenTaskBelongsToAnotherWorkspace()
    {
        var task = await GetForeignTask();

        var response = await Client.GetAsync($"api/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_ShouldLeaveTheTaskAlone_WhenTaskBelongsToAnotherWorkspace()
    {
        var task = await GetForeignTask();

        var response = await Client.DeleteAsync($"api/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var isDeleted = await IsTaskDeleted(task.Id);

        isDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTasks_ShouldSkipTasksFromAnotherWorkspace()
    {
        var task = await GetForeignTask();
        var request = new HttpRequestMessage(HttpMethod.Delete, "api/tasks")
        {
            Content = JsonContent.Create(new[] { task.Id }),
        };

        await Client.SendAsync(request, TestContext.Current.CancellationToken);

        var isDeleted = await IsTaskDeleted(task.Id);

        isDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProject_ShouldReturnNotFound_WhenProjectBelongsToAnotherWorkspace()
    {
        var project = await GetForeign(db => db.Projects);
        var request = new UpdateProjectRequest { Id = project.Id, Name = "Renamed across workspaces" };

        var response = await Client.PutAsJsonAsync("api/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var reloaded = await GetById(db => db.Projects, project.Id);

        reloaded.Name.Should().Be(project.Name);
    }

    [Fact]
    public async Task GetActivity_ShouldReturnNothing_WhenTaskBelongsToAnotherWorkspace()
    {
        var task = await GetForeignTask();

        var response = await Client.GetFromJsonAsync<ClientResponse<List<ActivityViewModel>>>(
            $"api/activity/Task/{task.Id}",
            TestContext.Current.CancellationToken);

        response.Payload.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBoard_ShouldReturnNotFound_WhenProjectBelongsToAnotherWorkspace()
    {
        var project = await GetForeign(db => db.Projects);
        var request = new AddBoardRequest
        {
            Name = "Cross workspace board",
            Identifier = $"cross-{Guid.NewGuid():N}"[..20],
            ProjectId = project.Id,
        };

        var response = await Client.PostAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoardsInProject_ShouldReturnNotFound_WhenProjectBelongsToAnotherWorkspace()
    {
        var project = await GetForeign(db => db.Projects);

        var response = await Client.GetAsync($"api/boards/project/{project.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldReturnNotFound_WhenBoardBelongsToAnotherWorkspace()
    {
        var board = await GetForeign(db => db.Boards);
        var request = new AddBoardGroupRequest { Name = "Cross workspace group", BoardId = board.Id };

        var response = await Client.PostAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldReturnNotFound_WhenGroupBelongsToAnotherWorkspace()
    {
        var group = await GetForeign(db => db.BoardGroups);
        var request = new UpdateBoardGroupRequest { BoardGroupId = group.Id, Name = "Renamed across workspaces" };

        var response = await Client.PutAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var reloaded = await GetById(db => db.BoardGroups, group.Id);

        reloaded.Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnNotFound_WhenGroupBelongsToAnotherWorkspace()
    {
        var group = await GetForeign(db => db.BoardGroups);
        var task = await GetForeignTask();
        var request = new MoveTasksToGroupRequest
        {
            BoardId = "unused",
            TaskIds = [task.Id],
            NewGroupId = group.Id,
        };

        var response = await Client.PostAsJsonAsync("api/tasks/move-tasks-to-group", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<ProjectTask> GetForeignTask()
    {
        return GetForeign(db => db.ProjectTasks);
    }

    private async Task<TEntity> GetForeign<TEntity>(Func<DataContext, IQueryable<TEntity>> set)
        where TEntity : Core.BaseEntities.WorkspaceEntity<int>
    {
        using var scope = Fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var entity = await set(db)
            .AsNoTracking()
            .Where(candidate => candidate.Workspace!.Slug == ForeignWorkspace && !candidate.IsDeleted)
            .OrderBy(candidate => candidate.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        return entity;
    }

    private async Task<TEntity> GetById<TEntity>(Func<DataContext, IQueryable<TEntity>> set, int id)
        where TEntity : Core.BaseEntities.WorkspaceEntity<int>
    {
        using var scope = Fixture.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var entity = await set(db)
            .AsNoTracking()
            .FirstAsync(candidate => candidate.Id == id, TestContext.Current.CancellationToken);

        return entity;
    }

    private async Task<bool> IsTaskDeleted(int taskId)
    {
        var task = await GetById(db => db.ProjectTasks, taskId);

        return task.IsDeleted;
    }
}
