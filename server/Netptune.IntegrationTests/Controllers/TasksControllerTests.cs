using System.Net;

using FluentAssertions;

using Xunit;

namespace Netptune.IntegrationTests.Controllers;

public class TasksControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public TasksControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

    }
}
