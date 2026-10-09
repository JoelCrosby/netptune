using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Entities.Contexts;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SprintsEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public SprintsEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
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
        secondStart.StatusCode.Should().Be(HttpStatusCode.BadRequest);

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
    public async Task GetBacklog_ShouldExcludeFinishedTasks()
    {
        var project = await CreateProject();
        var openTask = await CreateTask(project.Id);
        var doneTask = await CreateTask(project.Id);
        var doneStatusId = await GetStatusId(StatusCategory.Done);

        var updateResponse = await Client.PutAsJsonAsync("api/tasks", new UpdateProjectTaskRequest
        {
            Id = doneTask.Id,
            StatusId = doneStatusId,
        });
        updateResponse.EnsureSuccessStatusCode();

        var backlog = await GetBacklog(project.Id);

        backlog.Should().Contain(item => item.Id == openTask.Id);
        backlog.Should().NotContain(item => item.Id == doneTask.Id);
    }

    [Fact]
    public async Task GetBacklog_ShouldIncludeUnfinishedTasks_LeftOnAClosedSprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();
        (await Client.PostAsync($"api/sprints/{sprint.Id}/complete", null)).EnsureSuccessStatusCode();

        // Completion now hands unfinished tasks back, so strand one the way older completions did.
        using (var scope = Fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            await context.ProjectTasks
                .Where(item => item.Id == task.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.SprintId, sprint.Id), TestContext.Current.CancellationToken);
        }

        var backlog = await GetBacklog(project.Id);

        backlog.Should().Contain(item => item.Id == task.Id);
    }

    [Fact]
    public async Task Complete_ShouldReturnUnfinishedTasksToTheBacklog_WhenNoCarryOverSprintGiven()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        await AddTaskToSprint(sprint.Id, task.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync($"api/sprints/{sprint.Id}/complete", new CompleteSprintRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var backlog = await GetBacklog(project.Id);

        backlog.Should().ContainSingle(item => item.Id == task.Id)
            .Which.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Complete_ShouldMoveUnfinishedTasks_WhenCarryOverSprintGiven()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var nextSprint = await CreateSprint(project.Id, "Sprint 2");
        var task = await CreateTask(project.Id);

        await AddTaskToSprint(sprint.Id, task.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/complete",
            new CompleteSprintRequest { CarryOverSprintId = nextSprint.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await Client.GetFromJsonAsync<ClientResponse<SprintDetailViewModel>>($"api/sprints/{nextSprint.Id}");

        detail!.Payload!.Tasks.Should().ContainSingle(item => item.Id == task.Id);
    }

    [Fact]
    public async Task Complete_ShouldFail_AndLeaveTheSprintActive_WhenCarryOverSprintIsTheSameSprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);

        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/complete",
            new CompleteSprintRequest { CarryOverSprintId = sprint.Id });
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();
        var detail = await Client.GetFromJsonAsync<ClientResponse<SprintDetailViewModel>>($"api/sprints/{sprint.Id}");

        result!.IsSuccess.Should().BeFalse();
        detail!.Payload!.Status.Should().Be(SprintStatus.Active);
    }

    [Fact]
    public async Task Update_ShouldReturnUnfinishedTasksToTheBacklog_WhenSprintIsCancelled()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        await AddTaskToSprint(sprint.Id, task.Id);

        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = sprint.Id,
            Status = SprintStatus.Cancelled,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var backlog = await GetBacklog(project.Id);

        backlog.Should().ContainSingle(item => item.Id == task.Id)
            .Which.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldGiveEverySprintEventADistinctSequence_WhenActiveSprintIsCancelled()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var firstTask = await CreateTask(project.Id);
        var secondTask = await CreateTask(project.Id);
        var thirdTask = await CreateTask(project.Id);

        await AddTaskToSprint(sprint.Id, firstTask.Id);
        await AddTaskToSprint(sprint.Id, secondTask.Id);
        await AddTaskToSprint(sprint.Id, thirdTask.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = sprint.Id,
            Status = SprintStatus.Cancelled,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sprintSubjectType = EventEntityTypes.From(EntityType.Sprint);
        var sprintSubjectId = sprint.Id.ToString();
        var sequences = await context.EventRecords
            .Where(record => record.SubjectType == sprintSubjectType && record.SubjectId == sprintSubjectId)
            .Select(record => record.SubjectSequence)
            .ToListAsync(TestContext.Current.CancellationToken);
        var removedCount = await context.EventRecords
            .CountAsync(
                record => record.SubjectType == sprintSubjectType
                    && record.SubjectId == sprintSubjectId
                    && record.EventKey == EventKeys.ScopeMemberChanged
                    && record.Payload.RootElement.GetProperty("change").GetString() == "removed",
                TestContext.Current.CancellationToken);

        removedCount.Should().Be(3);
        sequences.Should().OnlyHaveUniqueItems();
        sequences.Should().BeEquivalentTo(Enumerable.Range(1, sequences.Count).Select(sequence => (long?)sequence));
    }

    [Fact]
    public async Task Update_ShouldCompleteThroughCompletion_WhenStatusSetToCompleted()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);

        await AddTaskToSprint(sprint.Id, task.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = sprint.Id,
            Name = "Renamed while completing",
            Status = SprintStatus.Completed,
        });
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();
        var backlog = await GetBacklog(project.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Payload!.Status.Should().Be(SprintStatus.Completed);
        result.Payload.Name.Should().Be("Renamed while completing");
        result.Payload.CompletedAt.Should().NotBeNull();
        backlog.Should().ContainSingle(item => item.Id == task.Id);
    }

    [Fact]
    public async Task Update_ShouldFail_WhenPlanningSprintIsSetToCompleted()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);

        var response = await Client.PutAsJsonAsync("api/sprints", new UpdateSprintRequest
        {
            Id = sprint.Id,
            Status = SprintStatus.Completed,
        });
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        result!.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Only active sprints can be completed");
    }

    [Fact]
    public async Task GetById_ShouldCountNewCategoryTasksAsNew()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);
        var newStatus = await CreateStatus(StatusCategory.New);

        var updateResponse = await Client.PutAsJsonAsync("api/tasks", new UpdateProjectTaskRequest
        {
            Id = task.Id,
            StatusId = newStatus.Id,
        });
        updateResponse.EnsureSuccessStatusCode();
        await AddTaskToSprint(sprint.Id, task.Id);

        var detail = await Client.GetFromJsonAsync<ClientResponse<SprintDetailViewModel>>($"api/sprints/{sprint.Id}");

        detail!.Payload!.NewTaskCount.Should().Be(1);
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

    [Fact]
    public async Task Get_ShouldCountArchivedTasksSeparately_WhenSprintTaskIsArchived()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);
        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        (await Client.DeleteAsync($"api/tasks/{task.Id}")).EnsureSuccessStatusCode();

        var detail = await Client.GetFromJsonAsync<ClientResponse<SprintDetailViewModel>>($"api/sprints/{sprint.Id}");

        detail.Payload!.TaskCount.Should().Be(0);
        detail.Payload.ArchivedTaskCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSprintTasks_ShouldKeepArchivedTasks_WhenIncludeArchivedRequested()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var task = await CreateTask(project.Id);
        await Client.PostAsJsonAsync(
            $"api/sprints/{sprint.Id}/tasks",
            new AddTasksToSprintRequest { TaskIds = [task.Id] });

        (await Client.DeleteAsync($"api/tasks/{task.Id}")).EnsureSuccessStatusCode();

        var listed = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(
            $"api/tasks?sprintId={sprint.Id}&pageSize=100&includeArchived=true");
        var withoutArchived = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(
            $"api/tasks?sprintId={sprint.Id}&pageSize=100");

        listed.Payload!.Items.Should().ContainSingle(item => item.Id == task.Id && item.IsArchived);
        withoutArchived.Payload!.Items.Should().BeEmpty();
    }

    private async Task AddTaskToSprint(int sprintId, int taskId)
    {
        var response = await Client.PostAsJsonAsync(
            $"api/sprints/{sprintId}/tasks",
            new AddTasksToSprintRequest { TaskIds = [taskId] });

        response.EnsureSuccessStatusCode();
    }

    private async Task<List<TaskViewModel>> GetBacklog(int projectId)
    {
        var response = await Client.GetAsync($"api/sprints/backlog?projectId={projectId}&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.IsSuccess.Should().BeTrue();

        return result.Payload!.Items.ToList();
    }

    private async Task<StatusViewModel> CreateStatus(StatusCategory category)
    {
        var response = await Client.PostAsJsonAsync("api/statuses", new CreateStatusRequest
        {
            Name = $"Sprint status {Guid.NewGuid():N}",
            Category = category,
        });
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<StatusViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task<int> GetStatusId(StatusCategory category)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Statuses
            .Where(status =>
                status.Workspace!.Slug == "netptune" &&
                status.EntityType == EntityType.Task &&
                status.Category == category &&
                !status.IsDeleted)
            .Select(status => status.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
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
