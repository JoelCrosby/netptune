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
public sealed class CalendarEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public CalendarEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetTasks_ShouldReturnTasksScheduledOnTheRequestedDay()
    {
        var status = await GetTaskStatus();
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Calendar spanning task",
            Description = "Task used to verify the calendar day query",
            StatusId = status.Id,
            ProjectId = 1,
            StartDate = new DateOnly(2026, 7, 10),
            DueDate = new DateOnly(2026, 7, 12),
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        var response = await Client.GetAsync(
            "api/calendar/tasks?date=2026-07-11&page=1&pageSize=25&sortBy=name&sortDirection=asc");
        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Page.Should().Be(1);
        result.Payload.PageSize.Should().Be(25);
        result.Payload.Items.Should().Contain(task => task.Id == created!.Payload!.Id);
        result.Payload.Items.Should().OnlyContain(task =>
            (task.StartDate ?? task.DueDate) <= new DateOnly(2026, 7, 11) &&
            (task.DueDate ?? task.StartDate) >= new DateOnly(2026, 7, 11));
    }

    [Fact]
    public async Task GetTasks_ShouldRejectAProjectOutsideTheWorkspaceScope()
    {
        var response = await Client.GetAsync("api/calendar/tasks?date=2026-07-11&projectId=2147483647");
        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetTasks_ShouldApplyTaskListFilters()
    {
        var status = await GetTaskStatus();
        var user = SeedData.Users.ElementAt(0);
        var createResponse = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = "Calendar uniquely filtered task",
            Description = "Task used to verify calendar task-list filters",
            StatusId = status.Id,
            ProjectId = 1,
            AssigneeId = user.Id,
            StartDate = new DateOnly(2026, 7, 18),
            DueDate = new DateOnly(2026, 7, 20),
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        var response = await Client.GetAsync(
            $"api/calendar/tasks?date=2026-07-19&search=uniquely%20filtered&statusIds={status.Id}&assignees={user.Id}");
        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Payload.Should().NotBeNull();
        result.Payload!.Items.Should().ContainSingle(task => task.Id == created!.Payload!.Id);
        result.Payload.Items.Should().OnlyContain(task =>
            task.StatusId == status.Id &&
            task.Assignees.Any(assignee => assignee.Id == user.Id));
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
