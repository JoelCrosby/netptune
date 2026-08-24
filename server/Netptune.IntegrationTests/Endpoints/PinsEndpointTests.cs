using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Pins;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Entities.Contexts;
using Netptune.Handlers.Pins.Commands;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PinsEndpointTests
{
    private const string BoardIdentifier = "neovim";

    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public PinsEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Create_ShouldBeIdempotent_AndReviveATombstone()
    {
        var task = await CreateTask("Pin idempotency");
        var first = await Pin(task.Id, TaskPinScope.User);
        var second = await Pin(task.Id, TaskPinScope.User);

        second.Id.Should().Be(first.Id, "pinning twice at the same scope must not create a second row");

        var rowsAfterSecondPin = await CountPins(task.Id);

        rowsAfterSecondPin.Should().Be(1);

        var deleteResponse = await Client.DeleteAsync($"api/pins/{first.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var revived = await Pin(task.Id, TaskPinScope.User);

        revived.Id.Should().Be(first.Id, "an unpinned row is revived rather than duplicated");

        var rowsAfterRevive = await CountPins(task.Id);

        rowsAfterRevive.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_ShouldReturnThePinnedTaskOnceWithEveryScope()
    {
        var task = await CreateTask("Pin scope stacking");
        var boardId = await GetBoardId();

        await Pin(task.Id, TaskPinScope.User);
        await Pin(task.Id, TaskPinScope.Board, boardId);
        await Pin(task.Id, TaskPinScope.Project);

        var pinned = await GetPinned();
        var entry = pinned.Should().ContainSingle(item => item.Task.Id == task.Id).Subject;

        entry.Pins.Select(pin => pin.Scope).Should().BeEquivalentTo(
            [TaskPinScope.User, TaskPinScope.Board, TaskPinScope.Project]);
        entry.Pins.Should().OnlyContain(pin => pin.CanUnpin);
        entry.Pins.Single(pin => pin.Scope == TaskPinScope.Board).ScopeName.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldDropPins_WhenTheTaskIsSoftDeleted()
    {
        var task = await CreateTask("Pin visibility");

        await Pin(task.Id, TaskPinScope.Workspace);

        var beforeDelete = await GetPinned();

        beforeDelete.Should().Contain(item => item.Task.Id == task.Id);

        var deleteResponse = await Client.DeleteAsync($"api/tasks/{task.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await GetPinned();

        afterDelete.Should().NotContain(item => item.Task.Id == task.Id);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnBoardProjectAndWorkspacePins()
    {
        var task = await CreateTask("Board banner pin");
        var boardId = await GetBoardId();

        await Pin(task.Id, TaskPinScope.Board, boardId);

        var response = await Client.GetAsync($"api/pins/board/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pinned = await response.Content.ReadFromJsonAsync<List<PinnedTaskViewModel>>();

        pinned!.Should().Contain(item => item.Task.Id == task.Id);
    }

    [Fact]
    public async Task GetBoard_ShouldNotFound_WhenTheBoardIsNotInTheWorkspace()
    {
        var response = await Client.GetAsync("api/pins/board/100000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BoardView_ShouldCarryThePinnedScopesForEachCard()
    {
        var task = await CreateTask("Board card badge");
        var boardId = await GetBoardId();

        await Pin(task.Id, TaskPinScope.User);
        await Pin(task.Id, TaskPinScope.Board, boardId);

        var tasks = await GetBoardViewTasks();
        var card = tasks.Should().ContainSingle(item => item.Id == task.Id).Subject;

        card.PinnedScopes.Should().BeEquivalentTo([TaskPinScope.User, TaskPinScope.Board]);

        var unpinned = await CreateTask("Board card without a badge");
        var refreshed = await GetBoardViewTasks();

        refreshed.Single(item => item.Id == unpinned.Id).PinnedScopes.Should().BeEmpty();
    }

    [Fact]
    public async Task Reorder_ShouldWriteTheGivenSortOrders()
    {
        var task = await CreateTask("Pin reorder");
        var pin = await Pin(task.Id, TaskPinScope.User);
        var request = new ReorderTaskPinsRequest { Items = [new TaskPinOrder(pin.Id, -42d)] };

        var response = await Client.PutAsJsonAsync("api/pins/reorder", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pinned = await GetPinned();
        var entry = pinned.Single(item => item.Task.Id == task.Id);

        entry.Pins.Single().SortOrder.Should().Be(-42d);
    }

    [Fact]
    public async Task Delete_ShouldNotFound_WhenThePinDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/pins/100000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<TaskPinViewModel> Pin(int taskId, TaskPinScope scope, int? scopeEntityId = null)
    {
        var request = new CreateTaskPinRequest
        {
            TaskId = taskId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
        };
        var response = await Client.PostAsJsonAsync("api/pins", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskPinViewModel>>();

        return result.Payload!;
    }

    private async Task<List<PinnedTaskViewModel>> GetPinned()
    {
        var response = await Client.GetAsync("api/pins");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<PinnedTaskViewModel>>();

        return result!;
    }

    private async Task<List<BoardViewTask>> GetBoardViewTasks()
    {
        var response = await Client.GetAsync($"api/boards/view/{BoardIdentifier}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();

        return [.. result.Payload!.Groups.SelectMany(group => group.Tasks)];
    }

    private async Task<TaskViewModel> CreateTask(string name)
    {
        var status = await GetTaskStatus();
        var boardGroupId = await GetBoardGroupId();
        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = $"{name} {Guid.NewGuid():N}",
            Description = "Created by the pins integration tests",
            StatusId = status.Id,
            ProjectId = 1,
            BoardGroupId = boardGroupId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return result.Payload!;
    }

    private async Task<int> CountPins(int taskId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.TaskPins.CountAsync(pin => pin.ProjectTaskId == taskId);
    }

    private async Task<int> GetBoardId()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var board = await context.Boards
            .Include(item => item.Workspace)
            .FirstAsync(item => item.Workspace!.Slug == "netptune" && item.Identifier == BoardIdentifier);

        return board.Id;
    }

    private async Task<int> GetBoardGroupId()
    {
        var boardId = await GetBoardId();

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var group = await context.BoardGroups
            .Where(item => item.BoardId == boardId && !item.IsDeleted)
            .OrderBy(item => item.SortOrder)
            .FirstAsync();

        return group.Id;
    }

    private async Task<Status> GetTaskStatus()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Statuses
            .Include(status => status.Workspace)
            .FirstAsync(status =>
                status.Workspace!.Slug == "netptune" &&
                status.EntityType == EntityType.Task &&
                status.Key == "in-progress");
    }
}
