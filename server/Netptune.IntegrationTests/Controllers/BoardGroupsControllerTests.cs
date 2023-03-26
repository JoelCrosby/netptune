using System.Net;
using System.Net.Http.Json;

using Xunit;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.IntegrationTests.Controllers;

[Collection(Collections.Database)]
public sealed class BoardGroupsControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public BoardGroupsControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boardgroups/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardGroup>();

        result!.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/boardgroups/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new UpdateBoardGroupRequest
        {
            BoardGroupId = 1,
            Name = "Updated name",
            SortOrder = 10,
        };

        var response = await Client.PutAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardGroupViewModel>>();

        result!.IsSuccess.Should().BeTrue();

        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateBoardGroupRequest
        {
            BoardGroupId = 1000,
            Name = "Updated name",
            SortOrder = 10,
        };

        var response = await Client.PutAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddBoardGroupRequest
        {
            Name = "new name",
            SortOrder = 2,
            Type = BoardGroupType.Basic,
            BoardId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<BoardGroupViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
        result.Payload.Type.Should().Be(request.Type);
        result.Payload.BoardId.Should().Be(request.BoardId);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddBoardGroupRequest
        {
            SortOrder = 2,
            Type = BoardGroupType.Basic,
            BoardId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/boardgroups", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.DeleteAsync("api/boardgroups/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/boardgroups/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeFalse();
    }
}
