using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Search;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SearchSystemIdTests
{
    private static readonly SemaphoreSlim ReindexGate = new(1, 1);

    private static bool SearchIndexReady;

    private readonly HttpClient Client;

    public SearchSystemIdTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Search_ShouldFindTask_WhenQueriedByExactSystemId()
    {
        var task = await CreateTask();

        var result = await Search(task.SystemId);

        result.Results.Should().Contain(item => item.Type == "task" && item.Id == task.Id);
    }

    [Fact]
    public async Task Search_ShouldFindTask_WhenQueriedByUppercaseSystemId()
    {
        var task = await CreateTask();

        var result = await Search(task.SystemId.ToUpperInvariant());

        result.Results.Should().Contain(item => item.Type == "task" && item.Id == task.Id);
    }

    [Fact]
    public async Task Tasks_ShouldFilterBySystemId_WhenSearchTermIsSystemId()
    {
        var task = await CreateTask();

        var response = await Client.GetFromJsonAsync<ClientResponse<PagedResponse<TaskViewModel>>>(
            $"api/tasks?search={Uri.EscapeDataString(task.SystemId)}");

        response.Payload!.Items.Should().Contain(item => item.Id == task.Id);
    }

    [Fact]
    public async Task BoardView_ShouldFilterBySystemId_WhenTermIsSystemId()
    {
        var task = await CreateTask();

        var response = await Client.GetFromJsonAsync<ClientResponse<BoardView>>(
            $"api/boards/view/neovim?term={Uri.EscapeDataString(task.SystemId)}");

        var tasks = response.Payload!.Groups.SelectMany(group => group.Tasks);

        tasks.Should().Contain(item => item.Id == task.Id);
    }

    private async Task<SearchResponse> Search(string query)
    {
        await EnsureSearchIndex();

        var response = await Client.GetFromJsonAsync<SearchResponse>(
            $"api/search?q={Uri.EscapeDataString(query)}");

        return response!;
    }

    // Indexing a task before the seed service has applied index settings lets meilisearch
    // auto-create the index without filterable attributes, which makes every search error.
    // Reindexing restores them, but only one reindex can be in flight at a time.
    private async Task EnsureSearchIndex()
    {
        if (SearchIndexReady)
        {
            return;
        }

        await ReindexGate.WaitAsync();

        try
        {
            if (SearchIndexReady)
            {
                return;
            }

            var response = await Client.PostAsync("api/search/reindex", null);

            response.EnsureSuccessStatusCode();

            SearchIndexReady = true;
        }
        finally
        {
            ReindexGate.Release();
        }
    }

    private async Task<TaskViewModel> CreateTask()
    {
        var request = new AddProjectTaskRequest
        {
            Name = $"System id fixture {Guid.NewGuid():N}",
            ProjectId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }
}
