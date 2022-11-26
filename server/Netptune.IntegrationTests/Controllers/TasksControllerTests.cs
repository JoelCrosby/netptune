using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.ViewModels.ProjectTasks;

using Xunit;

namespace Netptune.IntegrationTests.Controllers;

public sealed class TasksControllerTests : IClassFixture<NetptuneApiFactory>
{
    private readonly HttpClient Client;

    public TasksControllerTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<TaskViewModel>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetail_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/tasks/detail?systemId=kak-3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskViewModel>();

        result.Should().NotBeNull();
    }
}
