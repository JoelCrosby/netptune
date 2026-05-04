using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class TasksEndpointTests
{
    private readonly HttpClient Client;

    public TasksEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Page-Limit").Should().ContainSingle("50");

        var result = await response.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        result.Should().NotBeEmpty();
        result.Should().HaveCountLessThanOrEqualTo(50);
    }

    [Fact]
    public async Task Get_ShouldClampTake_WhenTakeExceedsMax()
    {
        var response = await Client.GetAsync("api/tasks?take=999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Page-Limit").Should().ContainSingle("100");

        var result = await response.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Get_ShouldReturnStableNextPage_WhenCursorProvided()
    {
        var firstResponse = await Client.GetAsync("api/tasks?take=2");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("X-Next-Cursor", out var cursorValues).Should().BeTrue();

        var cursor = cursorValues!.Single();
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        firstPage.Should().HaveCount(2);

        var secondResponse = await Client.GetAsync($"api/tasks?take=2&cursor={Uri.EscapeDataString(cursor)}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPage = await secondResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        secondPage.Should().NotBeNull();
        secondPage.Select(task => task.Id).Should().NotIntersectWith(firstPage.Select(task => task.Id));
    }

    [Fact]
    public async Task Get_ShouldFilterTasks_WhenFilterQueryProvided()
    {
        var searchResponse = await Client.GetAsync("api/tasks?search=OpenTelemetry");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await searchResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        searchResult.Should().NotBeNull();
        searchResult.Should().NotBeEmpty();
        searchResult.Should().OnlyContain(task =>
            task.Name.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
            task.ProjectName!.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
            task.Tags.Any(tag => tag.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase)));

        var tagResponse = await Client.GetAsync("api/tasks?tags=Typescript");

        tagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tagResult = await tagResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        tagResult.Should().NotBeNull();
        tagResult.Should().NotBeEmpty();
        tagResult.Should().OnlyContain(task => task.Tags.Contains("Typescript"));

        var statusResponse = await Client.GetAsync($"api/tasks?statuses={(int)ProjectTaskStatus.Complete}");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResult = await statusResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        statusResult.Should().NotBeNull();
        statusResult.Should().NotBeEmpty();
        statusResult.Should().OnlyContain(task => task.Status == ProjectTaskStatus.Complete);

        var user = SeedData.Users.ElementAt(0);
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Assignee filter test",
            Description = "Task used to verify assignee filtering",
            Status = ProjectTaskStatus.InProgress,
            ProjectId = 1,
            AssigneeId = user.Id,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var assigneeResponse = await Client.GetAsync($"api/tasks?assignees={user.Id}");

        assigneeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var assigneeResult = await assigneeResponse.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        assigneeResult.Should().NotBeNull();
        assigneeResult.Should().NotBeEmpty();
        assigneeResult.Should().OnlyContain(task =>
            task.Assignees.Any(assignee => assignee.Id == user.Id));
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
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Status.Should().Be(request.Status);
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
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Status.Should().Be(request.Status);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddProjectTaskRequest
        {
            Description = "new description",
            Status = ProjectTaskStatus.InProgress,
            ProjectId = 1,
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

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/tasks/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
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

        result.IsSuccess.Should().BeTrue();
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

        result.IsSuccess.Should().BeTrue();
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

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnSuccess_WhenInputValid()
    {
        var user = SeedData.Users.ElementAt(0);
        var request = new ReassignTasksRequest
        {
            TaskIds = new() { 0, 1 },
            BoardId = "neovim",
            AssigneeId = user.Id,
        };

        var response = await Client.PostAsJsonAsync("api/tasks/reassign-tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }
}
