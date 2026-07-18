using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Meta;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Sprints;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ReportingEndpointTests(NetptuneFixture fixture)
{
    private readonly HttpClient Client = fixture.CreateNetptuneClient();

    [Fact]
    public async Task Flow_ShouldReturnReport_WhenRangeIsValid()
    {
        var response = await Client.GetAsync("api/reports/flow?from=2026-01-01&to=2026-12-31&timeZone=UTC");

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
        report!.UniqueTaskCount.Should().BeGreaterThan(0);
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
        report!.SprintId.Should().Be(sprint.Id);
        report.Coverage.IsPartial.Should().BeFalse();
    }

    [Fact]
    public async Task Flow_ShouldRejectInvertedRange()
    {
        var response = await Client.GetAsync("api/reports/flow?from=2026-12-31&to=2026-01-01&timeZone=UTC");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ProjectViewModel> CreateProject()
    {
        var response = await Client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            Name = $"Report {Guid.NewGuid():N}"[..30],
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
}
