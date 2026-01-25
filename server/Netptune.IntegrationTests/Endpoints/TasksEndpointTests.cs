using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(Collections.Database)]
public sealed class TasksEndpointTests
{
    private readonly HttpClient Client;

    public TasksEndpointTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/tasks/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDetail_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks/detail?systemId=neo-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetail_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/tasks/detail?systemId=zzz-3");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDetail_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var response = await Client.GetAsync("api/tasks/detail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new UpdateProjectTaskRequest
        {
            Id = 1,
            Name = "updated name",
            Description = "updated description",
            Status = ProjectTaskStatus.Complete,
            IsFlagged = true,
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Status.Should().Be(request.Status);
        result.Payload.IsFlagged.Should().Be(request.IsFlagged.Value);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateProjectTaskRequest
        {
            Id = -1,
            Name = "updated name",
            Description = "updated description",
            Status = ProjectTaskStatus.Complete,
            IsFlagged = true,
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new UpdateProjectTaskRequest
        {
            Name = "updated name",
            Description = "updated description",
            Status = ProjectTaskStatus.Complete,
            IsFlagged = true,
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddProjectTaskRequest
        {
            Name = "new name",
            Description = "new description",
            Status = ProjectTaskStatus.InProgress,
            ProjectId = 1,
            IsFlagged = false,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Status.Should().Be(request.Status);
        result.Payload.IsFlagged.Should().Be(request.IsFlagged);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddProjectTaskRequest
        {
            Description = "new description",
            Status = ProjectTaskStatus.InProgress,
            ProjectId = 1,
            IsFlagged = false,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.DeleteAsync("api/tasks/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/tasks/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMany_ShouldReturnCorrectly_WhenInputValid()
    {
        var taskIds = new[] { 2, 4 };

        var request = new HttpRequestMessage
        {
            RequestUri = new ("api/tasks", UriKind.Relative),
            Method = HttpMethod.Delete,
            Content = JsonContent.Create(taskIds),
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldReturnSuccess_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 0,
            PreviousIndex = 0,
            TaskId = 7,
            NewGroupId = 1,
            OldGroupId = 0,
        };

        var response = await Client.PostAsJsonAsync("api/tasks/move-task-in-group", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnSuccess_WhenInputValid()
    {
        var request = new MoveTasksToGroupRequest
        {
            TaskIds = new() { 0, 1 },
            BoardId = "neovim",
            NewGroupId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/tasks/move-tasks-to-group", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnSuccess_WhenInputValid()
    {
        var user = TestData.Users.ElementAt(0);
        var request = new ReassignTasksRequest
        {
            TaskIds = new() { 0, 1 },
            BoardId = "neovim",
            AssigneeId = user.Id,
        };

        var response = await Client.PostAsJsonAsync("api/tasks/reassign-tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }
}
