using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Meta;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ReportingEndpointTests(NetptuneFixture fixture)
{
    private readonly HttpClient Client = fixture.CreateNetptuneClient();

    [Fact]
    public async Task Flow_ShouldReturnReport_WhenIanaTimeZoneIsValid()
    {
        var response = await Client.GetAsync("api/reports/flow?from=2026-01-01&to=2026-12-31&timeZone=Europe%2FLondon");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<FlowReport>()).Should().NotBeNull();
    }

    [Fact]
    public async Task Workload_ShouldReturnCurrentOpenTasks()
    {
        var response = await Client.GetAsync("api/reports/workload?unit=Tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var report = await response.Content.ReadFromJsonAsync<WorkloadReport>();
        report.Should().NotBeNull();
        report.UniqueTaskCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Velocity_ShouldReturnReport_ForVisibleProject()
    {
        var response = await Client.GetAsync("api/reports/velocity?projectId=1&unit=Tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<VelocityReport>()).Should().NotBeNull();
    }

    [Fact]
    public async Task Burndown_ShouldReturnBaseline_ForNewlyStartedSprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.GetAsync($"api/reports/sprints/{sprint.Id}/burndown?unit=Tasks&timeZone=UTC");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var report = await response.Content.ReadFromJsonAsync<SprintBurndownReport>();
        report.Should().NotBeNull();
        report.SprintId.Should().Be(sprint.Id);
        report.Coverage.IsPartial.Should().BeFalse();
    }

    [Fact]
    public async Task Burndown_IdealShouldUseStartingCommitment_WhenScopeIsAdded()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        await CreateTask(project.Id, sprint.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();
        await CreateTask(project.Id, sprint.Id);

        var response = await Client.GetAsync(
            $"api/reports/sprints/{sprint.Id}/burndown?unit=Tasks&timeZone=UTC");
        var report = await response.Content.ReadFromJsonAsync<SprintBurndownReport>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        report.Should().NotBeNull();
        report.CommittedCount.Should().Be(1);
        report.AddedCount.Should().Be(1);
        report.Points.Should().ContainSingle();
        report.Points.Single().TotalScope.Should().Be(2);
        report.Points.Single().Ideal.Should().Be(1);
    }

    [Fact]
    public async Task Velocity_ShouldOnlyCountTasksEnteringDoneDuringTheSprint()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        var doneStatus = await GetStatus("complete");
        await CreateTask(project.Id, sprint.Id, doneStatus.Id);
        var completedDuringSprint = await CreateTask(project.Id, sprint.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        var updateResponse = await Client.PutAsJsonAsync("api/tasks", new UpdateProjectTaskRequest
        {
            Id = completedDuringSprint.Id,
            StatusId = doneStatus.Id,
        });
        updateResponse.EnsureSuccessStatusCode();
        (await Client.PostAsync($"api/sprints/{sprint.Id}/complete", null)).EnsureSuccessStatusCode();

        var response = await Client.GetAsync(
            $"api/reports/velocity?projectId={project.Id}&unit=Tasks&take=20");
        var report = await response.Content.ReadFromJsonAsync<VelocityReport>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        report.Should().NotBeNull();
        report.Sprints.Single(point => point.SprintId == sprint.Id).Completed.Should().Be(1);
    }

    [Fact]
    public async Task Flow_ShouldFallBackToUtc_WhenTimeZoneIsInvalid()
    {
        var response = await Client.GetAsync(
            "api/reports/flow?from=2026-01-01&to=2026-12-31&timeZone=Not%2FAZone");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Flow_ShouldRejectInvertedRange()
    {
        var response = await Client.GetAsync("api/reports/flow?from=2026-12-31&to=2026-01-01&timeZone=UTC");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Flow_ShouldReturnNotFound_ForProjectInAnotherWorkspace()
    {
        var foreignProjectId = await GetProjectIdInWorkspace("linux");

        var response = await Client.GetAsync(
            $"api/reports/flow?from=2026-01-01&to=2026-12-31&timeZone=UTC&projectId={foreignProjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Velocity_ShouldReturnNotFound_ForProjectInAnotherWorkspace()
    {
        var foreignProjectId = await GetProjectIdInWorkspace("linux");

        var response = await Client.GetAsync($"api/reports/velocity?projectId={foreignProjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Burndown_ShouldReturnNotFound_ForSprintInAnotherWorkspace()
    {
        var project = await CreateProject();
        var sprint = await CreateSprint(project.Id);
        (await Client.PostAsync($"api/sprints/{sprint.Id}/start", null)).EnsureSuccessStatusCode();

        // The owning workspace can read it, proving the 404 below is isolation and not a bad sprint.
        (await Client.GetAsync($"api/reports/sprints/{sprint.Id}/burndown?unit=Tasks&timeZone=UTC"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var otherWorkspaceClient = CreateClientForWorkspace("linux");

        var response = await otherWorkspaceClient.GetAsync(
            $"api/reports/sprints/{sprint.Id}/burndown?unit=Tasks&timeZone=UTC");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClientForWorkspace(string slug)
    {
        var client = fixture.CreateNetptuneClient();
        client.DefaultRequestHeaders.Remove("workspace");
        client.DefaultRequestHeaders.Add("workspace", slug);

        return client;
    }

    private async Task<int> GetProjectIdInWorkspace(string slug)
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Projects
            .Where(project => project.Workspace.Slug == slug && !project.IsDeleted)
            .Select(project => project.Id)
            .FirstAsync();
    }

    private async Task<ProjectViewModel> CreateProject()
    {
        var response = await Client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            // Lead with the guid so the derived project key (first 4 chars of the name) is unique;
            // a shared "Report " prefix makes several projects collide on the workspace/key index.
            Name = $"{Guid.NewGuid():N} Report"[..30],
            Description = "Reporting integration test project",
            MetaInfo = new ProjectMeta { Color = "#3366ff" },
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        return result.Payload!;
    }

    private async Task<SprintViewModel> CreateSprint(int projectId)
    {
        var response = await Client.PostAsJsonAsync("api/sprints", new AddSprintRequest
        {
            Name = $"Reporting sprint {Guid.NewGuid():N}",
            Goal = "Verify reporting baseline",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(14),
            ProjectId = projectId,
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>();

        return result.Payload!;
    }

    private async Task<TaskViewModel> CreateTask(
        int projectId,
        int sprintId,
        int? statusId = null)
    {
        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = $"Reporting task {Guid.NewGuid():N}",
            Description = "Reporting integration test task",
            ProjectId = projectId,
            SprintId = sprintId,
            StatusId = statusId,
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return result.Payload!;
    }

    private async Task<StatusViewModel> GetStatus(string key)
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Statuses
            .Where(status =>
                status.Workspace.Slug == "netptune" &&
                status.EntityType == EntityType.Task &&
                status.Key == key)
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
