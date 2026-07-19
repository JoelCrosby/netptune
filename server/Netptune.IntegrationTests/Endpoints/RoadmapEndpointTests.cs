using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Roadmap;
using Netptune.Core.ViewModels.Relations;
using Netptune.Entities.Contexts;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class RoadmapEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public RoadmapEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnTasksThatOverlapTheInclusiveRange()
    {
        var status = await GetTaskStatus();
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Roadmap overlap task",
            Description = "Task used to verify the roadmap range query",
            StatusId = status.Id,
            ProjectId = 1,
            StartDate = new DateOnly(2026, 7, 1),
            DueDate = new DateOnly(2026, 7, 10),
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        var response = await Client.GetAsync("api/roadmap?from=2026-07-10&to=2026-07-12");
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.Tasks.Should().Contain(task => task.Id == created.Payload!.Id);
        roadmap.Truncated.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldReturnSeededScheduledTaskShapes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-60);
        var to = today.AddDays(60);
        var url = $"api/roadmap?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await Client.GetAsync(url);
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.Tasks.Should().Contain(task => task.StartDate.HasValue && task.DueDate.HasValue);
        roadmap.Tasks.Should().Contain(task => !task.StartDate.HasValue && task.DueDate.HasValue);
        roadmap.Tasks.Should().Contain(task => task.StartDate.HasValue && !task.DueDate.HasValue);
    }

    [Fact]
    public async Task Get_ShouldReturnDependenciesFromTasksOutsideTheDateRange()
    {
        var status = await GetTaskStatus();
        var blocker = await CreateTask("Roadmap off-range blocker", status.Id, new DateOnly(2026, 6, 1));
        var blocked = await CreateTask("Roadmap visible blocked task", status.Id, new DateOnly(2026, 7, 20));
        var relationTypeId = await GetRelationTypeId("blocks");
        var relationResponse = await Client.PostAsJsonAsync("api/task-relations", new CreateTaskRelationRequest
        {
            SourceSystemId = blocker.SystemId,
            TargetSystemId = blocked.SystemId,
            RelationTypeId = relationTypeId,
        });
        var relation = await relationResponse.Content.ReadFromJsonAsync<ClientResponse<TaskRelationViewModel>>();

        var response = await Client.GetAsync("api/roadmap?from=2026-07-20&to=2026-07-20");
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.Tasks.Should().Contain(task => task.Id == blocked.Id);
        roadmap.Tasks.Should().NotContain(task => task.Id == blocker.Id);
        roadmap.Relations.Should().Contain(item => item.Id == relation!.Payload!.Id);
    }

    [Fact]
    public async Task Get_ShouldRejectRangesLongerThan366Days()
    {
        var response = await Client.GetAsync("api/roadmap?from=2026-01-01&to=2027-01-02");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldRejectProjectsOutsideTheWorkspaceScope()
    {
        var response = await Client.GetAsync(
            "api/roadmap?from=2026-07-01&to=2026-07-31&projectIds=2147483647");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldRejectSprintsOutsideTheWorkspaceScope()
    {
        var response = await Client.GetAsync(
            "api/roadmap?from=2026-07-01&to=2026-07-31&sprintIds=2147483647");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUnscheduledTasks_ShouldReturnAPaginatedResult()
    {
        var response = await Client.GetAsync(
            "api/roadmap/unscheduled-tasks?page=1&pageSize=1&sortBy=name&sortDirection=asc");
        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(1);
        result.Payload.TotalCount.Should().BeGreaterThan(0);
        result.Payload.Items.Should().HaveCount(1);
        result.Payload.Items.Should().OnlyContain(task => !task.StartDate.HasValue && !task.DueDate.HasValue);
    }

    private async Task<Status> GetTaskStatus()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Statuses
            .Include(status => status.Workspace)
            .FirstAsync(status =>
                status.Workspace.Slug == "netptune" &&
                status.EntityType == EntityType.Task &&
                status.Key == "in-progress");
    }

    private async Task<TaskViewModel> CreateTask(string name, int statusId, DateOnly date)
    {
        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = name,
            Description = "Task used to verify off-range roadmap dependencies",
            StatusId = statusId,
            ProjectId = 1,
            StartDate = date,
            DueDate = date,
        });
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return result!.Payload!;
    }

    private async Task<int> GetRelationTypeId(string key)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var relationType = await context.RelationTypes
            .Include(type => type.Workspace)
            .FirstAsync(type => type.Workspace.Slug == "netptune" && type.Key == key);

        return relationType.Id;
    }
}
