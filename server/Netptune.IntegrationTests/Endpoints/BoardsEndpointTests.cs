using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// The board view filter tests place tasks of their own, so this class writes to workspace 1 and
// has to serialise with everything else that does -- an archive export taken while a task is
// being added captures a placement whose task is not in the same snapshot.
[Collection(WorkspaceMutationCollection.Name)]
public sealed class BoardsEndpointTests
{
    private readonly HttpClient Client;

    public BoardsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boards/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardViewModel>();

        result!.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/boards/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = await CreateBoard();
        var request = new UpdateBoardRequest
        {
            Id = board.Id,
            Name = "Updated name",
            Meta = new()
            {
                Color = "blue",
            },
        };

        var response = await Client.PutAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardViewModel>>();

        result.IsSuccess.Should().BeTrue();

        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.MetaInfo.Should().BeEquivalentTo(request.Meta);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateBoardRequest
        {
            Id = 1000,
            Name = "Updated name",
        };

        var response = await Client.PutAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddBoardRequest
        {
            Name = "new name",
            Identifier = "new-name",
            ProjectId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.Identifier.Should().Be(request.Identifier);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddBoardRequest
        {
            Identifier = "new-name",
        };

        var response = await Client.PostAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = await CreateBoard();

        var response = await Client.DeleteAsync($"api/boards/{board.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/boards/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoardsInWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boards/workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<BoardViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBoardsInProject_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boards/project/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<BoardViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBoardView_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boards/view/neovim");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBoardView_ShouldReturnCorrectly_WhenSearchTermProvided()
    {
        var response = await Client.GetAsync("api/boards/view/neovim?term=task");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
    }


    [Fact]
    public async Task GetBoardView_ShouldExcludeTaggedTasks_WhenHasTagsFalse()
    {
        var tagged = await SeedBoardTask("tagged", ["Typescript"]);
        var untagged = await SeedBoardTask("untagged", []);

        var tasks = await GetBoardViewTasks("api/boards/view/neovim?hasTags=false");

        tasks.Should().NotContain(task => task.Tags.Count > 0);
        tasks.Should().Contain(task => task.Id == untagged.Id);
        tasks.Should().NotContain(task => task.Id == tagged.Id);
    }

    [Fact]
    public async Task GetBoardView_ShouldExcludeUntaggedTasks_WhenHasTagsTrue()
    {
        var tagged = await SeedBoardTask("tagged", ["Typescript"]);
        var untagged = await SeedBoardTask("untagged", []);

        var tasks = await GetBoardViewTasks("api/boards/view/neovim?hasTags=true");

        tasks.Should().NotContain(task => task.Tags.Count == 0);
        tasks.Should().Contain(task => task.Id == tagged.Id);
        tasks.Should().NotContain(task => task.Id == untagged.Id);
    }

    // The seeded board columns hold no tasks, so these place one themselves. Without that the
    // assertions pass against an empty board and prove nothing about the filters.
    [Fact]
    public async Task GetBoardView_ShouldReturnOnlyMatchingStatuses_WhenStatusIdsProvided()
    {
        var seeded = await SeedBoardTask("status filter", ["Typescript"]);

        var tasks = await GetBoardViewTasks($"api/boards/view/neovim?statusIds={seeded.StatusId}");

        tasks.Should().NotBeEmpty();
        tasks.Should().OnlyContain(task => task.StatusId == seeded.StatusId);
        tasks.Should().Contain(task => task.Id == seeded.Id);
    }

    [Fact]
    public async Task GetBoardView_ShouldReturnOnlyMatchingAssignees_WhenUsersProvided()
    {
        var assignee = SeedData.Users.ElementAt(0);
        var seeded = await SeedBoardTask("assignee filter", ["Typescript"], assignee.Id);

        var tasks = await GetBoardViewTasks($"api/boards/view/neovim?users={assignee.Id}");

        tasks.Should().NotBeEmpty();
        tasks.Should().OnlyContain(task => task.Assignees.Any(user => user.Id == assignee.Id));
        tasks.Should().Contain(task => task.Id == seeded.Id);
    }

    [Fact]
    public async Task GetBoardView_ShouldReturnOnlyMatchingTags_WhenTagsProvided()
    {
        var seeded = await SeedBoardTask("tag filter", ["Typescript"]);

        var tasks = await GetBoardViewTasks("api/boards/view/neovim?tags=Typescript");

        tasks.Should().NotBeEmpty();
        tasks.Should().OnlyContain(task => task.Tags.Contains("Typescript"));
        tasks.Should().Contain(task => task.Id == seeded.Id);
    }

    [Fact]
    public async Task GetBoardView_ShouldReportTheColumnSize_WhenNothingIsTruncated()
    {
        await SeedBoardTask("column size", []);

        var response = await Client.GetAsync("api/boards/view/neovim");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();
        var groups = result.Payload!.Groups.ToList();

        groups.Should().NotBeEmpty();
        groups.Should().Contain(group => group.Tasks.Count > 0);
        groups.Should().OnlyContain(group => group.TotalTaskCount == group.Tasks.Count);
        groups.Should().OnlyContain(group => !group.IsTruncated);
    }

    private async Task<TaskViewModel> SeedBoardTask(string name, List<string> tags, string? assigneeId = null)
    {
        var view = await Client.GetFromJsonAsync<ClientResponse<BoardView>>("api/boards/view/neovim");
        var boardGroupId = view.Payload!.Groups.First().Id;

        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = $"Board view {name} {Guid.NewGuid():N}"[..48],
            Description = "Task used to verify board view filtering",
            ProjectId = 1,
            BoardGroupId = boardGroupId,
            Tags = tags,
            AssigneeIds = assigneeId is null ? null : [assigneeId],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return created.Payload!;
    }

    private async Task<List<BoardViewTask>> GetBoardViewTasks(string url)
    {
        var response = await Client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardView>>();

        result.IsSuccess.Should().BeTrue();

        return [.. result.Payload!.Groups.SelectMany(group => group.Tasks)];
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnFalse_WhenSlugUnique()
    {
        var response = await Client.GetAsync("api/boards/is-unique/neovim");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnTrue_WhenSlugIsNotUnique()
    {
        var response = await Client.GetAsync("api/boards/is-unique/not-a-real-slug");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    private async Task<BoardViewModel> CreateBoard()
    {
        var identifier = $"delete-target-{Guid.NewGuid():N}"[..24];
        var response = await Client.PostAsJsonAsync("api/boards", new AddBoardRequest
        {
            Name = "Delete target",
            Identifier = identifier,
            ProjectId = 1,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardViewModel>>();

        return result.Payload!;
    }
}
