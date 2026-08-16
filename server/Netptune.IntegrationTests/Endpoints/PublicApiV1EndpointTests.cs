using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Users;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PublicApiV1EndpointTests
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);

    private static PublicApiTestSetup? Setup;

    private readonly NetptuneFixture Fixture;

    public PublicApiV1EndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    private sealed record PublicApiTestSetup(string ApiKey, int ProjectId);

    private static readonly string[] Permissions =
    [
        NetptunePermissions.Projects.Read,
        NetptunePermissions.Members.Read,
        NetptunePermissions.Statuses.Read,
        NetptunePermissions.Sprints.Read,
        NetptunePermissions.Sprints.Create,
        NetptunePermissions.Sprints.Update,
        NetptunePermissions.Sprints.Delete,
        NetptunePermissions.Sprints.ManageTasks,
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Tasks.Create,
        NetptunePermissions.Tasks.Update,
        NetptunePermissions.Tasks.Move,
        NetptunePermissions.Tags.Assign,
        NetptunePermissions.BoardGroups.Read,
    ];

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenNoCredentialIsPresented()
    {
        var client = Fixture.CreateUnauthenticatedPublicApiClient();

        var response = await client.GetAsync("api/v1/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldReturnACorrelationId()
    {
        var client = Fixture.CreateUnauthenticatedPublicApiClient();

        var response = await client.GetAsync("api/v1/tasks", TestContext.Current.CancellationToken);

        response.Headers.Should().ContainKey("X-Correlation-Id");
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheCredentialIsNotRecognised()
    {
        var client = Fixture.CreatePublicApiClient("ntp_not-a-real-credential");

        var response = await client.GetAsync("api/v1/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_ShouldReturnTheWorkspaceProjects()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/projects?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<List<ProjectViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAssignees_ShouldReturnTheWorkspaceMembers()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/assignees?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<AssigneeViewModel>>();

        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStatuses_ShouldReturnTheWorkspaceStatuses()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/statuses?entityType=Task");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<List<StatusViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTasks_ShouldReturnAPageOfTasks()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/tasks?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnOnlyUntaggedTasks_WhenHasTagsIsFalse()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/tasks?hasTags=false&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeEmpty();
        result.Payload.Items.Should().OnlyContain(task => task.Tags.Count == 0);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnCreated_AndBeReadableById()
    {
        var client = await CreateClient();
        var setup = await GetSetup();
        var name = $"Public API task {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("api/v1/tasks", new AddProjectTaskRequest
        {
            Name = name,
            Description = "Created by the public API integration test.",
            ProjectId = setup.ProjectId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        created!.Name.Should().Be(name);
        response.Headers.Location!.ToString().Should().Be($"/api/v1/tasks/{created.Id}");

        var fetched = await client.GetFromJsonAsync<TaskViewModel>($"api/v1/tasks/{created.Id}");

        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetTask_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/tasks/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTask_ShouldReplaceTheSuppliedFields()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);
        var name = $"Renamed public API task {Guid.NewGuid():N}";

        var response = await client.PatchAsJsonAsync($"api/v1/tasks/{task.Id}", new UpdateProjectTaskRequest
        {
            Id = task.Id,
            Name = name,
            Description = task.Description,
            StatusId = task.StatusId,
            OwnerId = task.OwnerId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        result!.Name.Should().Be(name);
    }

    [Fact]
    public async Task BulkUpdateTasks_ShouldReturnNoContent_AndApplyThePriority()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);

        var response = await client.PostAsJsonAsync("api/v1/tasks/bulk-update", new
        {
            taskIds = new[] { task.Id },
            priority = TaskPriority.High,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        var updated = await client.GetFromJsonAsync<TaskViewModel>($"api/v1/tasks/{task.Id}");

        updated!.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task SprintLifecycle_ShouldCreateReadUpdateManageTasksAndDelete()
    {
        var client = await CreateClient();
        var setup = await GetSetup();
        var name = $"Public API sprint {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("api/v1/sprints", new AddSprintRequest
        {
            Name = name,
            Goal = "Created by the public API integration test.",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(14),
            ProjectId = setup.ProjectId,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());

        var sprint = (await createResponse.Content.ReadFromJsonAsync<SprintViewModel>())!;

        sprint.Name.Should().Be(name);

        var list = await client.GetFromJsonAsync<List<SprintViewModel>>($"api/v1/sprints?projectId={setup.ProjectId}");

        list!.Should().Contain(item => item.Id == sprint.Id);

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/sprints/{sprint.Id}", new UpdateSprintRequest
        {
            Goal = "Updated by the public API integration test.",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
        (await updateResponse.Content.ReadFromJsonAsync<SprintViewModel>())!
            .Goal.Should().Be("Updated by the public API integration test.");

        var task = await CreateTask(client);

        var addTasksResponse = await client.PostAsJsonAsync(
            $"api/v1/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        addTasksResponse.StatusCode.Should().Be(HttpStatusCode.OK, await addTasksResponse.Content.ReadAsStringAsync());
        (await addTasksResponse.Content.ReadFromJsonAsync<SprintDetailViewModel>())!
            .Tasks.Should().Contain(item => item.Id == task.Id);

        var detail = await client.GetFromJsonAsync<SprintDetailViewModel>($"api/v1/sprints/{sprint.Id}");

        detail!.Id.Should().Be(sprint.Id);
        detail.Tasks.Should().Contain(item => item.Id == task.Id);

        var removeResponse = await client.DeleteAsync($"api/v1/sprints/{sprint.Id}/tasks/{task.Id}");

        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK, await removeResponse.Content.ReadAsStringAsync());
        (await removeResponse.Content.ReadFromJsonAsync<SprintDetailViewModel>())!
            .Tasks.Should().NotContain(item => item.Id == task.Id);

        var deleteResponse = await client.DeleteAsync($"api/v1/sprints/{sprint.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());

        (await client.GetAsync($"api/v1/sprints/{sprint.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSprint_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/sprints/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTaskFromSprint_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.DeleteAsync("api/v1/sprints/999999/tasks/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoardGroups_ShouldReturnTheColumnsForTheWorkspace()
    {
        var client = await CreateClient();
        var setup = await GetSetup();

        var response = await client.GetAsync("api/v1/board-groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<List<BoardGroupOptionViewModel>>();

        result!.Should().NotBeEmpty();
        result.Should().Contain(group => group.ProjectId == setup.ProjectId);
        result.Should().OnlyContain(group => !string.IsNullOrWhiteSpace(group.Name));
    }

    [Fact]
    public async Task UpdateTask_ShouldMoveTheTaskIntoTheSuppliedBoardGroup()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);
        var target = await FindOtherBoardGroup(client, task);

        var response = await client.PatchAsJsonAsync($"api/v1/tasks/{task.Id}", new
        {
            boardGroupId = target.Id,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var moved = await client.GetFromJsonAsync<TaskViewModel>($"api/v1/tasks/{task.Id}");

        moved!.BoardGroupId.Should().Be(target.Id);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturnNotFound_WhenTheBoardGroupDoesNotExist()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);

        var response = await client.PatchAsJsonAsync($"api/v1/tasks/{task.Id}", new
        {
            boardGroupId = 999999,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BulkUpdateTasks_ShouldMoveTheTasksIntoTheSuppliedBoardGroup()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);
        var target = await FindOtherBoardGroup(client, task);

        var response = await client.PostAsJsonAsync("api/v1/tasks/bulk-update", new
        {
            taskIds = new[] { task.Id },
            boardGroupId = target.Id,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        var moved = await client.GetFromJsonAsync<TaskViewModel>($"api/v1/tasks/{task.Id}");

        moved!.BoardGroupId.Should().Be(target.Id);
    }

    private async Task<BoardGroupOptionViewModel> FindOtherBoardGroup(HttpClient client, TaskViewModel task)
    {
        var setup = await GetSetup();
        var groups = await client.GetFromJsonAsync<List<BoardGroupOptionViewModel>>("api/v1/board-groups");
        var target = groups!.FirstOrDefault(group =>
            group.ProjectId == setup.ProjectId && group.Id != task.BoardGroupId);

        target.Should().NotBeNull("the test project needs a second board column to move tasks into");

        return target;
    }

    private async Task<HttpClient> CreateClient()
    {
        var setup = await GetSetup();

        return Fixture.CreatePublicApiClient(setup.ApiKey);
    }

    private async Task<TaskViewModel> CreateTask(HttpClient client)
    {
        var setup = await GetSetup();
        var response = await client.PostAsJsonAsync("api/v1/tasks", new AddProjectTaskRequest
        {
            Name = $"Public API task {Guid.NewGuid():N}",
            Description = "Created by the public API integration test.",
            ProjectId = setup.ProjectId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TaskViewModel>())!;
    }

    private async Task<PublicApiTestSetup> GetSetup()
    {
        if (Setup is not null)
        {
            return Setup;
        }

        await SetupLock.WaitAsync();

        try
        {
            Setup ??= new PublicApiTestSetup(await CreateApiKey(), await CreateProjectId());

            return Setup;
        }
        finally
        {
            SetupLock.Release();
        }
    }

    private async Task<string> CreateApiKey()
    {
        var client = Fixture.CreateNetptuneClient();

        var accountResponse = await client.PostAsJsonAsync("api/service-accounts", new CreateServiceAccountRequest
        {
            Name = $"Public API agent {Guid.NewGuid():N}",
            Description = "Created by the public API integration test.",
            Permissions = Permissions,
        });

        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK, await accountResponse.Content.ReadAsStringAsync());

        var account = (await accountResponse.Content.ReadFromJsonAsync<ServiceAccountViewModel>())!;

        var credentialResponse = await client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            new CreateApiCredentialRequest
            {
                Name = "Public API integration credential",
                Scopes = Permissions,
            });

        credentialResponse.StatusCode.Should().Be(HttpStatusCode.OK, await credentialResponse.Content.ReadAsStringAsync());

        var credential = (await credentialResponse.Content.ReadFromJsonAsync<ApiCredentialCreatedViewModel>())!;

        credential.Token.Should().StartWith("ntp_");

        return credential.Token;
    }

    private async Task<int> CreateProjectId()
    {
        var client = Fixture.CreateNetptuneClient();

        var response = await client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            Name = $"Public API project {Guid.NewGuid():N}",
            Description = "Created by the public API integration test.",
            MetaInfo = new() { Color = "blue" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        return result.Payload!.Id;
    }
}
