using System.Net;
using System.Net.Http.Json;

using Xunit;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(Collections.Database)]
public sealed class BoardsEndpointTests
{
    private readonly HttpClient Client;

    public BoardsEndpointTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
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
        var request = new UpdateBoardRequest
        {
            Id = 1,
            Name = "Updated name",
        };

        var response = await Client.PutAsJsonAsync("api/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardViewModel>>();

        result!.IsSuccess.Should().BeTrue();

        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.Name);
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

        result!.IsSuccess.Should().BeTrue();
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
        var response = await Client.DeleteAsync("api/boards/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/boards/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeFalse();
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

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
    }


    [Fact]
    public async Task IsSlugUnique_ShouldReturnFalse_WhenSlugUnique()
    {
        var response = await Client.GetAsync("api/boards/is-unique/neovim");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnTrue_WhenSlugIsNotUnique()
    {
        var response = await Client.GetAsync("api/boards/is-unique/not-a-real-slug");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeTrue();
    }
}
