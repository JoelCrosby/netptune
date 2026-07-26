using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Search;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SearchEndpointTests
{
    private static readonly TimeSpan IndexTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public SearchEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Reindex_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.PostAsync("api/search/reindex", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ShouldReturnTheHydratedTask_AfterReindex()
    {
        var task = await CreateTask();

        var result = await ReindexAndWaitForTask(task);

        var hit = result.Results.Single(item => item.Type == "task" && item.Id == task.Id);

        // The project key is not on the task view model; it is the system id without its scope suffix.
        var projectKey = task.SystemId[..^$"-{task.ProjectScopeId}".Length];

        hit.Title.Should().Be(task.Name);
        hit.Url.Should().Be($"/netptune/tasks/{task.SystemId}");
        hit.Subtitle.Should().Be($"{projectKey} · {task.StatusName}");
        hit.Metadata["projectKey"]!.ToString().Should().Be(projectKey);
    }

    [Fact]
    public async Task Search_ShouldRespectTypeFilter_WhenTypesSupplied()
    {
        var task = await CreateTask();

        await ReindexAndWaitForTask(task);

        var response = await Client.GetAsync(
            $"api/search?q={Uri.EscapeDataString(task.Name)}&types=projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();

        result!.Results.Should().NotContain(item => item.Type == "task");
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenQueryMissing()
    {
        var response = await Client.GetAsync("api/search");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();

        result!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldNotLeakTasksFromAnotherWorkspace()
    {
        var task = await CreateTask();

        await ReindexAndWaitForTask(task);

        var linuxClient = Fixture.CreateNetptuneClient();

        linuxClient.DefaultRequestHeaders.Remove("workspace");
        linuxClient.DefaultRequestHeaders.Add("workspace", "linux");

        var response = await linuxClient.GetAsync($"api/search?q={Uri.EscapeDataString(task.Name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();

        result!.Results.Should().NotContain(item => item.Id == task.Id);
    }

    private async Task<SearchResponse> ReindexAndWaitForTask(TaskViewModel task)
    {
        (await Client.PostAsync("api/search/reindex", null)).EnsureSuccessStatusCode();

        var url = $"api/search?q={Uri.EscapeDataString(task.Name)}";
        var deadline = DateTime.UtcNow + IndexTimeout;

        while (true)
        {
            var result = await Client.GetFromJsonAsync<SearchResponse>(url);

            if (result!.Results.Any(item => item.Type == "task" && item.Id == task.Id))
            {
                return result;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"'{task.Name}' was not searchable within {IndexTimeout}.");
            }

            await Task.Delay(PollInterval);
        }
    }

    private async Task<TaskViewModel> CreateTask()
    {
        var request = new AddProjectTaskRequest
        {
            Name = $"Search fixture {Guid.NewGuid():N}",
            Description = "Task for search integration tests",
            ProjectId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
