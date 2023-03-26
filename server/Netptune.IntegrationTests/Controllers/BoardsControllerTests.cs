using System.Net;
using System.Net.Http.Json;

using Xunit;

using FluentAssertions;

using Netptune.Core.ViewModels.Boards;

namespace Netptune.IntegrationTests.Controllers;

[Collection(Collections.Database)]
public sealed class BoardsControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public BoardsControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/boards/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardViewModel>();

        result!.Should().NotBeNull();
    }
}
