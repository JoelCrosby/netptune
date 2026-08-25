using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Core.ViewModels.Tags;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Users;

using StackExchange.Redis;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ApiV1EndpointTests
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);

    private static ApiTestSetup? Setup;

    private readonly NetptuneFixture Fixture;

    public ApiV1EndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    private sealed record ApiTestSetup(string ApiKey, int ProjectId);

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
        NetptunePermissions.Workspace.Read,
        NetptunePermissions.Projects.Create,
        NetptunePermissions.Projects.Update,
        NetptunePermissions.Projects.Delete,
        NetptunePermissions.Boards.Read,
        NetptunePermissions.Boards.Create,
        NetptunePermissions.Boards.Update,
        NetptunePermissions.Boards.Delete,
        NetptunePermissions.BoardGroups.Create,
        NetptunePermissions.BoardGroups.Update,
        NetptunePermissions.BoardGroups.Delete,
        NetptunePermissions.Tags.Read,
        NetptunePermissions.Tags.Create,
        NetptunePermissions.Tags.Update,
        NetptunePermissions.Tags.Delete,
        NetptunePermissions.Comments.Read,
        NetptunePermissions.Comments.Create,
        NetptunePermissions.Comments.DeleteOwn,
        NetptunePermissions.RelationTypes.Read,
        NetptunePermissions.RelationTypes.Manage,
        NetptunePermissions.Statuses.Manage,
        NetptunePermissions.Tasks.Delete,
        NetptunePermissions.Tasks.Restore,
        NetptunePermissions.Tasks.Reassign,
        NetptunePermissions.Flags.Read,
    ];

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenNoCredentialIsPresented()
    {
        var client = Fixture.CreateUnauthenticatedApiClient();

        var response = await client.GetAsync("api/v1/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldReturnACorrelationId()
    {
        var client = Fixture.CreateUnauthenticatedApiClient();

        var response = await client.GetAsync("api/v1/tasks", TestContext.Current.CancellationToken);

        response.Headers.Should().ContainKey("X-Correlation-Id");
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheCredentialIsNotRecognised()
    {
        var client = Fixture.CreateApiClient("ntp_not-a-real-credential");

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
        var name = $"API task {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("api/v1/tasks", new AddProjectTaskRequest
        {
            Name = name,
            Description = "Created by the API integration test.",
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
        var name = $"Renamed API task {Guid.NewGuid():N}";

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
        var name = $"API sprint {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("api/v1/sprints", new AddSprintRequest
        {
            Name = name,
            Goal = "Created by the API integration test.",
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
            Goal = "Updated by the API integration test.",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
        (await updateResponse.Content.ReadFromJsonAsync<SprintViewModel>())!
            .Goal.Should().Be("Updated by the API integration test.");

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


    [Fact]
    public async Task CreateProject_ShouldDeriveADistinctKey_WhenNamesShareALeadingPrefix()
    {
        var client = await CreateClient();
        var sharedName = $"Shared prefix project {Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync("api/v1/projects", new AddProjectRequest
        {
            Name = sharedName,
            MetaInfo = new() { Color = "blue" },
        }, TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());

        var second = await client.PostAsJsonAsync("api/v1/projects", new AddProjectRequest
        {
            Name = sharedName,
            MetaInfo = new() { Color = "blue" },
        }, TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Created, await second.Content.ReadAsStringAsync());

        var firstProject = (await first.Content.ReadFromJsonAsync<ProjectViewModel>(TestContext.Current.CancellationToken))!;
        var secondProject = (await second.Content.ReadFromJsonAsync<ProjectViewModel>(TestContext.Current.CancellationToken))!;

        secondProject.Key.Should().NotBe(firstProject.Key);
    }

    [Fact]
    public async Task CreateProject_ShouldSucceed_ForANameShorterThanAProjectKey()
    {
        var client = await CreateClient();

        var response = await client.PostAsJsonAsync("api/v1/projects", new AddProjectRequest
        {
            Name = "ab",
            MetaInfo = new() { Color = "blue" },
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_ShouldBroadcastAWorkspaceEvent_SoOpenClientsSeeIt()
    {
        var client = await CreateClient();
        var connection = Fixture.ApiServices.GetRequiredService<IConnectionMultiplexer>();
        var channel = RedisChannel.Literal(IWorkspaceEventPublisher.ChannelName);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var queue = await connection.GetSubscriber().SubscribeAsync(channel);

        queue.OnMessage(message => received.TrySetResult(message.Message.ToString()));

        try
        {
            await CreateTask(client);

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));

            completed.Should().Be(received.Task, "creating a task should publish a workspace event");

            var published = JsonSerializer.Deserialize<JsonElement>(await received.Task);

            published.GetProperty("scopes").EnumerateArray()
                .Select(scope => scope.GetString())
                .Should().Contain(WorkspaceEventScopes.Task);

            published.GetProperty("workspace").GetString().Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            await connection.GetSubscriber().UnsubscribeAsync(channel);
        }
    }

    [Fact]
    public async Task OpenApiDocument_ShouldDescribeEveryPublishedRoute()
    {
        var client = Fixture.CreateUnauthenticatedApiClient();

        var response = await client.GetAsync("openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var document = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var paths = document.GetProperty("paths");

        var described = paths.EnumerateObject().Select(path => path.Name).ToList();

        described.Should().Contain(
        [
            "/api/v1/workspace",
            "/api/v1/boards",
            "/api/v1/board-groups",
            "/api/v1/tags",
            "/api/v1/relation-types",
            "/api/v1/comments/{id}",
            "/api/v1/search",
            "/api/v1/reports/flow",
        ]);
    }

    [Fact]
    public async Task GetWorkspace_ShouldReturnTheCredentialsWorkspace()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/workspace", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var workspace = await response.Content.ReadFromJsonAsync<WorkspaceViewModel>(TestContext.Current.CancellationToken);

        workspace!.Slug.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetBoards_ShouldReturnTheWorkspaceBoards()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/boards?pageSize=100", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var boards = await response.Content.ReadFromJsonAsync<List<BoardViewModel>>(TestContext.Current.CancellationToken);

        boards.Should().NotBeNull();
        boards.Should().OnlyContain(board => board.ProjectId > 0);
    }

    [Fact]
    public async Task BoardLifecycle_ShouldCreateReadUpdateAndDelete()
    {
        var client = await CreateClient();
        var board = await CreateBoard(client);

        var getResponse = await client.GetAsync($"api/v1/boards/{board.Id}", TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());

        var viewResponse = await client.GetAsync($"api/v1/boards/{board.Identifier}/view", TestContext.Current.CancellationToken);

        viewResponse.StatusCode.Should().Be(HttpStatusCode.OK, await viewResponse.Content.ReadAsStringAsync());

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/boards/{board.Id}", new
        {
            name = "API board renamed",
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        var updated = (await updateResponse.Content.ReadFromJsonAsync<BoardViewModel>(TestContext.Current.CancellationToken))!;

        updated.Name.Should().Be("API board renamed");

        var deleteResponse = await client.DeleteAsync($"api/v1/boards/{board.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());

        (await client.GetAsync($"api/v1/boards/{board.Id}", TestContext.Current.CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BoardGroupLifecycle_ShouldCreateReadUpdateAndDelete()
    {
        var client = await CreateClient();
        var board = await CreateBoard(client);

        var createResponse = await client.PostAsJsonAsync("api/v1/board-groups", new AddBoardGroupRequest
        {
            Name = "API column",
            BoardId = board.Id,
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());

        var group = (await createResponse.Content.ReadFromJsonAsync<BoardGroupViewModel>(TestContext.Current.CancellationToken))!;

        var getResponse = await client.GetAsync($"api/v1/board-groups/{group.Id}", TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/board-groups/{group.Id}", new
        {
            name = "API column renamed",
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        var updated = (await updateResponse.Content.ReadFromJsonAsync<BoardGroupViewModel>(TestContext.Current.CancellationToken))!;

        updated.Name.Should().Be("API column renamed");

        var deleteResponse = await client.DeleteAsync($"api/v1/board-groups/{group.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());

        (await client.GetAsync($"api/v1/board-groups/{group.Id}", TestContext.Current.CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TagLifecycle_ShouldCreateAssignListRenameAndDelete()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);
        var tag = $"api-{Guid.NewGuid():N}"[..20];


        var createResponse = await client.PostAsJsonAsync("api/v1/tags", new AddTagRequest
        {
            Tag = tag,
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());

        var created = (await createResponse.Content.ReadFromJsonAsync<TagViewModel>(TestContext.Current.CancellationToken))!;

        tag = created.Name;

        var listResponse = await client.GetAsync("api/v1/tags?pageSize=100", TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK, await listResponse.Content.ReadAsStringAsync());

        var tags = await listResponse.Content.ReadFromJsonAsync<List<TagViewModel>>(TestContext.Current.CancellationToken);

        tags.Should().Contain(item => item.Name == tag);

        var assignResponse = await client.PutAsync($"api/v1/tasks/{task.Id}/tags/{tag}", null, TestContext.Current.CancellationToken);

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, await assignResponse.Content.ReadAsStringAsync());

        var taskTagsResponse = await client.GetAsync($"api/v1/tasks/{task.Id}/tags", TestContext.Current.CancellationToken);

        taskTagsResponse.StatusCode.Should().Be(HttpStatusCode.OK, await taskTagsResponse.Content.ReadAsStringAsync());

        var taskTags = await taskTagsResponse.Content.ReadFromJsonAsync<List<TagViewModel>>(TestContext.Current.CancellationToken);

        taskTags.Should().Contain(item => item.Name == tag);

        var renamed = $"{tag}-renamed";

        var renameResponse = await client.PatchAsJsonAsync($"api/v1/tags/{tag}", new
        {
            newValue = renamed,
        }, TestContext.Current.CancellationToken);

        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK, await renameResponse.Content.ReadAsStringAsync());

        var removeResponse = await client.DeleteAsync(
            $"api/v1/tasks/{task.Id}/tags/{renamed}",
            TestContext.Current.CancellationToken);

        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await removeResponse.Content.ReadAsStringAsync());

        var deleteResponse = await client.DeleteAsync($"api/v1/tags/{renamed}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CommentLifecycle_ShouldCreateListUpdateReactAndDelete()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);

        var createResponse = await client.PostAsJsonAsync($"api/v1/tasks/{task.Id}/comments", new
        {
            comment = "Created by the API integration test.",
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());

        var comment = (await createResponse.Content.ReadFromJsonAsync<CommentViewModel>(TestContext.Current.CancellationToken))!;

        var listResponse = await client.GetAsync($"api/v1/tasks/{task.Id}/comments", TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK, await listResponse.Content.ReadAsStringAsync());

        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentViewModel>>(TestContext.Current.CancellationToken);

        comments.Should().Contain(item => item.Id == comment.Id);

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/comments/{comment.Id}", new
        {
            comment = "Updated by the API integration test.",
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        var reactResponse = await client.PutAsync(
            $"api/v1/comments/{comment.Id}/reactions/{Uri.EscapeDataString("👍")}",
            null,
            TestContext.Current.CancellationToken);

        reactResponse.StatusCode.Should().Be(HttpStatusCode.OK, await reactResponse.Content.ReadAsStringAsync());

        var unreactResponse = await client.DeleteAsync(
            $"api/v1/comments/{comment.Id}/reactions/{Uri.EscapeDataString("👍")}",
            TestContext.Current.CancellationToken);

        unreactResponse.StatusCode.Should().Be(HttpStatusCode.OK, await unreactResponse.Content.ReadAsStringAsync());

        var deleteResponse = await client.DeleteAsync($"api/v1/comments/{comment.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RelationLifecycle_ShouldCreateATypeLinkTwoTasksAndUnlinkThem()
    {
        var client = await CreateClient();
        var source = await CreateTask(client);
        var target = await CreateTask(client);

        var createTypeResponse = await client.PostAsJsonAsync("api/v1/relation-types", new CreateRelationTypeRequest
        {
            Name = $"API relates to {Guid.NewGuid():N}"[..40],
            InverseName = "API related from",
        }, TestContext.Current.CancellationToken);

        createTypeResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createTypeResponse.Content.ReadAsStringAsync());

        var relationType = (await createTypeResponse.Content.ReadFromJsonAsync<RelationTypeViewModel>(TestContext.Current.CancellationToken))!;

        var typesResponse = await client.GetAsync("api/v1/relation-types", TestContext.Current.CancellationToken);

        typesResponse.StatusCode.Should().Be(HttpStatusCode.OK, await typesResponse.Content.ReadAsStringAsync());

        var types = await typesResponse.Content.ReadFromJsonAsync<List<RelationTypeViewModel>>(TestContext.Current.CancellationToken);

        types.Should().Contain(item => item.Id == relationType.Id);

        var linkResponse = await client.PostAsJsonAsync($"api/v1/tasks/{source.Id}/relations", new AddTaskRelationRequest
        {
            RelatedSystemId = target.SystemId,
            RelationTypeId = relationType.Id,
        }, TestContext.Current.CancellationToken);

        linkResponse.StatusCode.Should().Be(HttpStatusCode.Created, await linkResponse.Content.ReadAsStringAsync());

        var relation = (await linkResponse.Content.ReadFromJsonAsync<TaskRelationViewModel>(TestContext.Current.CancellationToken))!;

        var relationsResponse = await client.GetAsync($"api/v1/tasks/{source.Id}/relations", TestContext.Current.CancellationToken);

        relationsResponse.StatusCode.Should().Be(HttpStatusCode.OK, await relationsResponse.Content.ReadAsStringAsync());

        var relations = await relationsResponse.Content.ReadFromJsonAsync<List<TaskRelationViewModel>>(TestContext.Current.CancellationToken);

        relations.Should().Contain(item => item.Id == relation.Id);

        var unlinkResponse = await client.DeleteAsync($"api/v1/task-relations/{relation.Id}", TestContext.Current.CancellationToken);

        unlinkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await unlinkResponse.Content.ReadAsStringAsync());

        var usageResponse = await client.GetAsync(
            $"api/v1/relation-types/{relationType.Id}/usage",
            TestContext.Current.CancellationToken);

        usageResponse.StatusCode.Should().Be(HttpStatusCode.OK, await usageResponse.Content.ReadAsStringAsync());

        var deleteTypeResponse = await client.DeleteAsync(
            $"api/v1/relation-types/{relationType.Id}",
            TestContext.Current.CancellationToken);

        deleteTypeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteTypeResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProjectLifecycle_ShouldCreateReadUpdateAndDelete()
    {
        var client = await CreateClient();
        var project = await CreateProject(client);

        var getResponse = await client.GetAsync($"api/v1/projects/{project.Key}", TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/projects/{project.Id}", new
        {
            description = "Updated by the API integration test.",
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        var updated = (await updateResponse.Content.ReadFromJsonAsync<ProjectViewModel>(TestContext.Current.CancellationToken))!;

        updated.Description.Should().Be("Updated by the API integration test.");

        var deleteResponse = await client.DeleteAsync($"api/v1/projects/{project.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StatusLifecycle_ShouldCreateUpdateReportUsageAndDelete()
    {
        var client = await CreateClient();

        var createResponse = await client.PostAsJsonAsync("api/v1/statuses", new CreateStatusRequest
        {
            Name = $"API status {Guid.NewGuid():N}"[..30],
            Category = StatusCategory.Backlog,
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());

        var status = (await createResponse.Content.ReadFromJsonAsync<StatusViewModel>(TestContext.Current.CancellationToken))!;

        var updateResponse = await client.PatchAsJsonAsync($"api/v1/statuses/{status.Id}", new
        {
            name = "API status renamed",
            category = StatusCategory.Backlog,
        }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        var usageResponse = await client.GetAsync($"api/v1/statuses/{status.Id}/usage", TestContext.Current.CancellationToken);

        usageResponse.StatusCode.Should().Be(HttpStatusCode.OK, await usageResponse.Content.ReadAsStringAsync());

        var deleteResponse = await client.DeleteAsync($"api/v1/statuses/{status.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteTask_ShouldArchiveTheTask_AndRestoreShouldBringItBack()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);

        var deleteResponse = await client.DeleteAsync($"api/v1/tasks/{task.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync());

        var archivedResponse = await client.GetAsync("api/v1/tasks/archived?pageSize=100", TestContext.Current.CancellationToken);

        archivedResponse.StatusCode.Should().Be(HttpStatusCode.OK, await archivedResponse.Content.ReadAsStringAsync());

        var archived = await archivedResponse.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(TestContext.Current.CancellationToken);

        archived!.Payload!.Items.Should().Contain(item => item.Id == task.Id);

        var restoreResponse = await client.PostAsJsonAsync("api/v1/tasks/restore", new
        {
            taskIds = new[] { task.Id },
        }, TestContext.Current.CancellationToken);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await restoreResponse.Content.ReadAsStringAsync());

        (await client.GetAsync($"api/v1/tasks/{task.Id}", TestContext.Current.CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MoveTasks_ShouldPlaceTheTaskInTheSuppliedBoardGroup()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);
        var boardGroup = await GetFirstBoardGroup(client);

        var response = await client.PostAsJsonAsync("api/v1/tasks/move", new
        {
            taskIds = new[] { task.Id },
            boardGroupId = boardGroup.Id,
            position = 0,
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        var moved = await client.GetFromJsonAsync<TaskViewModel>(
            $"api/v1/tasks/{task.Id}",
            TestContext.Current.CancellationToken);

        moved!.Placements.Should().ContainSingle(placement => placement.BoardGroupId == boardGroup.Id);
    }

    [Fact]
    public async Task GetStatusBreakdown_ShouldReturnTheWorkspaceBreakdown()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/tasks/status-breakdown", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetTaskFlags_ShouldReturnTheFlagsRaisedAgainstATask()
    {
        var client = await CreateClient();
        var task = await CreateTask(client);

        var response = await client.GetAsync($"api/v1/tasks/{task.Id}/flags", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("api/v1/reports/flow")]
    [InlineData("api/v1/reports/workload")]
    public async Task Reports_ShouldReturnOk(string route)
    {
        var client = await CreateClient();

        var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetVelocityReport_ShouldReturnOk_ForTheTestProject()
    {
        var client = await CreateClient();
        var setup = await GetSetup();

        var response = await client.GetAsync(
            $"api/v1/reports/velocity?projectId={setup.ProjectId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_ShouldRejectAnEmptyTerm()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/search?q=", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnOk_ForATerm()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/search?q=public&limit=5", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<BoardViewModel> CreateBoard(HttpClient client)
    {
        var project = await CreateProject(client);
        var identifier = $"pub-{Guid.NewGuid():N}"[..12];

        var response = await client.PostAsJsonAsync("api/v1/boards", new AddBoardRequest
        {
            Name = "API board",
            Identifier = identifier,
            ProjectId = project.Id,
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BoardViewModel>(TestContext.Current.CancellationToken))!;
    }

    private async Task<ProjectViewModel> CreateProject(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("api/v1/projects", new AddProjectRequest
        {
            Name = $"{Guid.NewGuid():N} API created project",
            Description = "Created by the API integration test.",
            MetaInfo = new() { Color = "green" },
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ProjectViewModel>(TestContext.Current.CancellationToken))!;
    }

    private async Task<BoardGroupOptionViewModel> GetFirstBoardGroup(HttpClient client)
    {
        var groups = await client.GetFromJsonAsync<List<BoardGroupOptionViewModel>>(
            "api/v1/board-groups",
            TestContext.Current.CancellationToken);

        groups.Should().NotBeNullOrEmpty();

        return groups![0];
    }

    private async Task<HttpClient> CreateClient()
    {
        var setup = await GetSetup();

        return Fixture.CreateApiClient(setup.ApiKey);
    }

    private async Task<TaskViewModel> CreateTask(HttpClient client)
    {
        var setup = await GetSetup();
        var response = await client.PostAsJsonAsync("api/v1/tasks", new AddProjectTaskRequest
        {
            Name = $"API task {Guid.NewGuid():N}",
            Description = "Created by the API integration test.",
            ProjectId = setup.ProjectId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TaskViewModel>())!;
    }

    private async Task<ApiTestSetup> GetSetup()
    {
        if (Setup is not null)
        {
            return Setup;
        }

        await SetupLock.WaitAsync();

        try
        {
            Setup ??= new ApiTestSetup(await CreateApiKey(), await CreateProjectId());

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
            Name = $"API agent {Guid.NewGuid():N}",
            Description = "Created by the API integration test.",
            Permissions = Permissions,
        });

        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK, await accountResponse.Content.ReadAsStringAsync());

        var account = (await accountResponse.Content.ReadFromJsonAsync<ServiceAccountViewModel>())!;

        var credentialResponse = await client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            new CreateApiCredentialRequest
            {
                Name = "API integration credential",
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
            Name = $"API project {Guid.NewGuid():N}",
            Description = "Created by the API integration test.",
            MetaInfo = new() { Color = "blue" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        return result.Payload!.Id;
    }
}
