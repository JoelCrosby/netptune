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

        var response = await Client.GetAsync(
            "api/roadmap?from=2026-07-10&to=2026-07-12&includeUnscheduled=false");
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.Tasks.Should().Contain(task => task.Id == created.Payload!.Id);
        roadmap.UnscheduledTasks.Should().BeEmpty();
        roadmap.Truncated.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldReturnSeededScheduledTaskShapes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-60);
        var to = today.AddDays(60);
        var url = $"api/roadmap?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&includeUnscheduled=false";
        var response = await Client.GetAsync(url);
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.Tasks.Should().Contain(task => task.StartDate.HasValue && task.DueDate.HasValue);
        roadmap.Tasks.Should().Contain(task => !task.StartDate.HasValue && task.DueDate.HasValue);
        roadmap.Tasks.Should().Contain(task => task.StartDate.HasValue && !task.DueDate.HasValue);
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
    public async Task Get_ShouldReturnTheBoundedUnscheduledSummary()
    {
        var response = await Client.GetAsync("api/roadmap?from=2026-07-01&to=2026-07-31");
        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapViewModel>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        roadmap.Should().NotBeNull();
        roadmap!.UnscheduledCount.Should().BeGreaterThan(0);
        roadmap.UnscheduledTasks.Should().HaveCountLessThanOrEqualTo(200);
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
}
