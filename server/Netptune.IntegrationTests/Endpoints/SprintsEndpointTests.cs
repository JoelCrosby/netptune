using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Sprints;
using Netptune.TestData;
using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SprintsEndpointTests
{
    private readonly HttpClient Client;

    public SprintsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();
        var request = CreateSprintRequest(project.Id);

        var response = await Client.PostAsJsonAsync("api/sprints", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.ProjectId.Should().Be(project.Id);
        result.Payload.Status.Should().Be(SprintStatus.Planning);
    }

    [Fact]
    public async Task Start_ShouldRejectSecondActiveSprint_WhenProjectAlreadyHasActiveSprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id, "Sprint A");
        var secondSprint = await CreateSprint(project.Id, "Sprint B");

        var firstStart = await Client.PostAsync($"api/sprints/{sprint.Id}/start", null);
        var secondStart = await Client.PostAsync($"api/sprints/{secondSprint.Id}/start", null);

        firstStart.StatusCode.Should().Be(HttpStatusCode.OK);
        secondStart.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await secondStart.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("active sprint");
    }

    [Fact]
    public async Task AddTask_ShouldAssignTaskToSprint_WhenProjectMatches()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        var response = await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintDetailViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Tasks.Should().ContainSingle(item => item.Id == task.Id);
        result.Payload.Tasks.Single(item => item.Id == task.Id).SprintId.Should().Be(sprint.Id);
    }

    [Fact]
    public async Task Complete_ShouldReturnCorrectly_WhenSprintActive()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);

        await Client.PostAsync($"api/sprints/{sprint.Id}/start", null);
        var response = await Client.PostAsync($"api/sprints/{sprint.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Status.Should().Be(SprintStatus.Completed);
        result.Payload.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrent_ShouldReturnMostRecentActiveSprint_WhenCallerIsAssignedNoneOfItsTasks()
    {
        var project = await CreateProject();

        // A far-future start date guarantees this sprint sorts ahead of any
        // other active sprint in the shared workspace, so the assertion stays
        // deterministic regardless of what other tests create.
        var request = new AddSprintRequest
        {
            Name = $"Current Sprint {Guid.NewGuid():N}",
            Goal = "Sprint overview",
            StartDate = DateTime.UtcNow.Date.AddYears(5),
            EndDate = DateTime.UtcNow.Date.AddYears(5).AddDays(14),
            ProjectId = project.Id,
        };

        var createResponse = await Client.PostAsJsonAsync("api/sprints", request);
        var sprint = (await createResponse.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>()).Payload!;

        var callerId = await GetCallerUserId(project.Id);
        var otherUser = SeedData.Users.First(user => user.Id != callerId);
        var task = await CreateTask(project.Id, otherUser.Id);

        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        await Client.PostAsync($"api/sprints/{sprint.Id}/start", null);

        var response = await Client.GetAsync("api/sprints/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintDetailViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull("the running sprint is workspace scoped, not scoped to the caller's assignments");
        result.Payload!.Id.Should().Be(sprint.Id);
        result.Payload.Status.Should().Be(SprintStatus.Active);
        result.Payload.Tasks.Should().Contain(item => item.Id == task.Id);
        result.Payload.Tasks.Should().OnlyContain(item => item.Assignees.All(assignee => assignee.Id != callerId));
    }

    [Fact]
    public async Task Get_ShouldReturnSprintsForProject_WhenProjectIdSupplied()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);

        var response = await Client.GetAsync($"api/sprints?projectId={project.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<SprintViewModel>>();

        result!.Should().ContainSingle(item => item.Id == sprint.Id);
        result.Should().OnlyContain(item => item.ProjectId == project.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        var response = await Client.GetAsync($"api/sprints/{sprint.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintDetailViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Id.Should().Be(sprint.Id);
        result.Payload.Tasks.Should().Contain(item => item.Id == task.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var response = await Client.GetAsync("api/sprints/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBacklog_ShouldReturnTasksWithoutASprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var backlogTask = await CreateTask(project.Id);
        var sprintTask = await CreateTask(project.Id);

        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [sprintTask.Id] });

        var response = await Client.GetAsync($"api/sprints/backlog?projectId={project.Id}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().Contain(item => item.Id == backlogTask.Id);
        result.Payload.Items.Should().NotContain(item => item.Id == sprintTask.Id);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var name = $"Renamed sprint {Guid.NewGuid():N}";

        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = sprint.Id,
            Name = name,
            Goal = "Updated goal",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.Goal.Should().Be("Updated goal");
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = 999999,
            Name = "Missing sprint",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTask_ShouldDetachTaskFromSprint_WhenInputValid()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        var response = await Client.DeleteAsync($"api/sprints/{sprint.Id}/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintDetailViewModel>>();

        result.IsSuccess.Should().BeTrue();

        var reloaded = await Client.GetFromJsonAsync<ClientResponse<SprintDetailViewModel>>($"api/sprints/{sprint.Id}");

        reloaded.Payload!.Tasks.Should().NotContain(item => item.Id == task.Id);

        var backlog = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(
            $"api/sprints/backlog?projectId={project.Id}&pageSize=100");

        backlog.Payload!.Items.Should().Contain(item => item.Id == task.Id);
    }

    [Fact]
    public async Task RemoveTask_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/sprints/999999/tasks/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);

        var response = await Client.DeleteAsync($"api/sprints/{sprint.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        (await Client.GetAsync($"api/sprints/{sprint.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/sprints/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ProjectViewModel> CreateProject()
    {
        var request = new AddProjectRequest
        {
            Name = $"{Guid.NewGuid():N} Sprint Test",
            Description = "Project for sprint integration tests",
            MetaInfo = new()
            {
                Color = "blue",
            },
        };

        var response = await Client.PostAsJsonAsync("api/projects", request);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private static AddSprintRequest CreateSprintRequest(int projectId, string name = "Sprint 1")
    {
        return new()
        {
            Name = $"{name} {Guid.NewGuid():N}",
            Goal = "Ship sprint support",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(14),
            ProjectId = projectId,
        };
    }

    private async Task<SprintViewModel> CreateSprint(int projectId, string name = "Sprint 1")
    {
        var response = await Client.PostAsJsonAsync("api/sprints", CreateSprintRequest(projectId, name));
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task<string> GetCallerUserId(int projectId)
    {
        var task = await CreateTask(projectId);

        return task.Assignees.Single().Id;
    }

    private async Task<TaskViewModel> CreateTask(int projectId, string? assigneeId = null)
    {
        var request = new AddProjectTaskRequest
        {
            Name = $"Sprint task {Guid.NewGuid():N}",
            Description = "Task for sprint integration tests",
            ProjectId = projectId,
            AssigneeId = assigneeId,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
