using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Comments;

using Xunit;

namespace Netptune.IntegrationTests.Controllers;

[Collection(Collections.Database)]
public sealed class CommentsControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public CommentsControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/comments/task/neo-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CommentViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/comments/task/neo-1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddCommentRequest
        {
            Comment = "New Comment",
            SystemId = "neo-1",
        };

        var response = await Client.PostAsJsonAsync("api/comments/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload!.Body.Should().Be(request.Comment);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddCommentRequest
        {
            SystemId = "neo-1",
        };

        var response = await Client.PostAsJsonAsync("api/comments/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.DeleteAsync("api/comments/3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/comments/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result!.IsSuccess.Should().BeFalse();
    }
}
