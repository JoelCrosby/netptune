using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Entities.Contexts;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class TasksEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public TasksEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().HaveCountLessThanOrEqualTo(50);
        result.Payload.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(50);
        result.Payload.TotalCount.Should().BeGreaterThanOrEqualTo(result.Payload.Items.Count);
    }

    [Fact]
    public async Task Get_ShouldClampPageSize_WhenPageSizeExceedsMax()
    {
        var response = await Client.GetAsync("api/tasks?pageSize=999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.PageSize.Should().Be(100);
        result.Payload.Items.Should().HaveCountLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Get_ShouldReturnStableNextPage_WhenPageProvided()
    {
        var firstResponse = await Client.GetAsync("api/tasks?page=1&pageSize=2");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPage = await firstResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        firstPage.IsSuccess.Should().BeTrue();
        firstPage.Payload!.Items.Should().HaveCount(2);

        var secondResponse = await Client.GetAsync("api/tasks?page=2&pageSize=2");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPage = await secondResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        secondPage.IsSuccess.Should().BeTrue();
        secondPage.Payload!.Items.Should().NotBeNull();
        secondPage.Payload.Items.Select(task => task.Id).Should().NotIntersectWith(firstPage.Payload.Items.Select(task => task.Id));
    }

    [Fact]
    public async Task Get_ShouldFilterTasks_WhenFilterQueryProvided()
    {
        var searchResponse = await Client.GetAsync("api/tasks?search=OpenTelemetry");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await searchResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        searchResult.IsSuccess.Should().BeTrue();
        searchResult.Payload!.Items.Should().NotBeEmpty();
        searchResult.Payload.Items.Should().OnlyContain(task =>
            task.Name.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
            task.ProjectName!.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
            task.Tags.Any(tag => tag.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase)));

        var tagResponse = await Client.GetAsync("api/tasks?tags=Typescript");

        tagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tagResult = await tagResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        tagResult.IsSuccess.Should().BeTrue();
        tagResult.Payload!.Items.Should().NotBeEmpty();
        tagResult.Payload.Items.Should().OnlyContain(task => task.Tags.Contains("Typescript"));

        var completeStatus = await GetStatus("complete");
        var inProgressStatus = await GetStatus("in-progress");

        var statusResponse = await Client.GetAsync($"api/tasks?statusIds={completeStatus.Id}");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Payload!.Items.Should().NotBeEmpty();
        statusResult.Payload.Items.Should().OnlyContain(task => task.StatusId == completeStatus.Id);

        var user = SeedData.Users.ElementAt(0);
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Assignee filter test",
            Description = "Task used to verify assignee filtering",
            StatusId = inProgressStatus.Id,
            ProjectId = 1,
            AssigneeId = user.Id,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var assigneeResponse = await Client.GetAsync($"api/tasks?assignees={user.Id}");

        assigneeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var assigneeResult = await assigneeResponse.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        assigneeResult.IsSuccess.Should().BeTrue();
        assigneeResult.Payload!.Items.Should().NotBeEmpty();
        assigneeResult.Payload.Items.Should().OnlyContain(task =>
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
        var completeStatus = await GetStatus("complete");
        var request = new UpdateProjectTaskRequest
        {
            Id = 1,
            Name = "updated name",
            Description = "updated description",
            StatusId = completeStatus.Id,
            DueDate = new DateOnly(2026, 7, 31),
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.StatusId.Should().Be(completeStatus.Id);
        result.Payload.StatusKey.Should().Be(completeStatus.Key);
        result.Payload.DueDate.Should().Be(request.DueDate);

        var clearResponse = await Client.PutAsJsonAsync("api/tasks", new UpdateProjectTaskRequest
        {
            Id = request.Id,
            DueDate = null,
        });
        var cleared = await clearResponse.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        cleared.IsSuccess.Should().BeTrue();
        cleared.Payload!.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateProjectTaskRequest
        {
            Id = -1,
            Name = "updated name",
            Description = "updated description",
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
        };

        var response = await Client.PutAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var inProgressStatus = await GetStatus("in-progress");
        var request = new AddProjectTaskRequest
        {
            Name = "new name",
            Description = "new description",
            StatusId = inProgressStatus.Id,
            ProjectId = 1,
            DueDate = new DateOnly(2026, 8, 15),
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.StatusId.Should().Be(inProgressStatus.Id);
        result.Payload.StatusKey.Should().Be(inProgressStatus.Key);
        result.Payload.DueDate.Should().Be(request.DueDate);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddProjectTaskRequest
        {
            Description = "new description",
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
    public async Task Archive_ShouldListDeletedTasksAndRestoreThem_WhenInputValid()
    {
        var inProgressStatus = await GetStatus("in-progress");
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Archive restore test",
            Description = "Task used to verify the archive and restore flow",
            StatusId = inProgressStatus.Id,
            ProjectId = 1,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();
        var taskId = created.Payload!.Id;

        (await GetArchivedTasks()).Should().NotContain(task => task.Id == taskId);

        var deleteResponse = await Client.DeleteAsync($"api/tasks/{taskId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var archived = await GetArchivedTasks();

        archived.Should().Contain(task => task.Id == taskId);
        archived.Single(task => task.Id == taskId).DeletedByUsername.Should().NotBeNullOrEmpty();

        (await GetTasks()).Should().NotContain(task => task.Id == taskId);

        var restoreResponse = await Client.PostAsJsonAsync("api/tasks/restore", new[] { taskId });

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var restoreResult = await restoreResponse.Content.ReadFromJsonAsync<ClientResponse>();

        restoreResult.IsSuccess.Should().BeTrue();

        (await GetTasks()).Should().Contain(task => task.Id == taskId);
        (await GetArchivedTasks()).Should().NotContain(task => task.Id == taskId);
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

        var boardView = await GetBoardView("neovim");
        boardView.Groups
            .Single(group => group.Id == request.NewGroupId)
            .Tasks.Should()
            .Contain(task => task.Id == request.TaskId);

        var activity = await GetActivity(EntityType.Task, request.TaskId, ActivityType.Move);
        activity.Should().NotBeNull();
        activity.Meta.Should().NotBeNull();
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

    private Task<IReadOnlyList<TaskViewModel>> GetTasks()
    {
        return GetTaskPage("api/tasks?pageSize=100");
    }

    private Task<IReadOnlyList<TaskViewModel>> GetArchivedTasks()
    {
        return GetTaskPage("api/tasks/archive?pageSize=100");
    }

    private async Task<IReadOnlyList<TaskViewModel>> GetTaskPage(string url)
    {
        var response = await Client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!.Items;
    }

    private async Task<BoardView> GetBoardView(string boardIdentifier)
    {
        var response = await Client.GetAsync($"api/boards/view/{boardIdentifier}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task<ActivityViewModel?> GetActivity(EntityType entityType, int entityId, ActivityType activityType)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await Client.GetAsync($"api/activity/{entityType}/{entityId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<ActivityViewModel>>>();
            var activity = result.Payload?.FirstOrDefault(item => item.Type == activityType);

            if (activity is not null)
            {
                return activity;
            }

            await Task.Delay(100);
        }

        return null;
    }

    private async Task<StatusViewModel> GetStatus(string key)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Statuses
            .Where(status => status.Workspace.Slug == "netptune" && status.EntityType == EntityType.Task && status.Key == key)
            .Select(status => new StatusViewModel
            {
                Id = status.Id,
                WorkspaceId = status.WorkspaceId,
                EntityType = status.EntityType,
                Name = status.Name,
                Key = status.Key,
                Description = status.Description,
                Color = status.Color,
                SortOrder = status.SortOrder,
                Category = status.Category,
                IsSystem = status.IsSystem,
            })
            .SingleAsync();
    }
}
