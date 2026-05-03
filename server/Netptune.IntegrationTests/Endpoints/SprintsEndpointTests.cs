using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Sprints;
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

    private async Task<ProjectViewModel> CreateProject()
    {
        var request = new AddProjectRequest
        {
            Name = $"{Guid.NewGuid():N} Sprint Test",
            Description = "Project for sprint integration tests",
            MetaInfo = new()
            {
                Color = "#3b82f6",
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

    private async Task<TaskViewModel> CreateTask(int projectId)
    {
        var request = new AddProjectTaskRequest
        {
            Name = $"Sprint task {Guid.NewGuid():N}",
            Description = "Task for sprint integration tests",
            ProjectId = projectId,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
