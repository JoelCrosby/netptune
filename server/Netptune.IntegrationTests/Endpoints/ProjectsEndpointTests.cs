using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ProjectsEndpointTests
{
    private readonly HttpClient Client;

    public ProjectsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<ProjectViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("name")]
    [InlineData("key")]
    [InlineData("description")]
    [InlineData("owner")]
    [InlineData("updatedAt")]
    public async Task Get_ShouldSortCorrectly_WhenSortByProvided(string sortBy)
    {
        var response = await Client.GetAsync($"api/projects?sortBy={sortBy}&sortDirection=desc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<ProjectViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReverseOrder_WhenSortDirectionFlipped()
    {
        var ascendingResponse = await Client.GetAsync("api/projects?sortBy=name&sortDirection=asc");
        var descendingResponse = await Client.GetAsync("api/projects?sortBy=name&sortDirection=desc");

        var ascending = await ascendingResponse.Content.ReadFromJsonAsync<List<ProjectViewModel>>();
        var descending = await descendingResponse.Content.ReadFromJsonAsync<List<ProjectViewModel>>();

        var ascendingNames = ascending!.Select(project => project.Name).ToList();
        var descendingNames = descending!.Select(project => project.Name).ToList();

        // Another test in this collection can create a project between the two calls, so compare
        // only the names both responses saw.
        var sharedAscending = ascendingNames.Where(descendingNames.Contains).ToList();
        var sharedDescending = descendingNames.Where(ascendingNames.Contains).ToList();

        sharedAscending.Should().NotBeEmpty();
        sharedDescending.Should().Equal(sharedAscending.AsEnumerable().Reverse());
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/projects/neo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectViewModel>();

        result!.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/projects/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();
        var request = new UpdateProjectRequest
        {
            Id = project.Id,
            Name = "Updated name",
            Description = "Updated Description",
        };

        var response = await Client.PutAsJsonAsync("api/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        result.IsSuccess.Should().BeTrue();

        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var request = new UpdateProjectRequest
        {
            Id = 1000,
            Name = "Updated name",
        };

        var response = await Client.PutAsJsonAsync("api/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddProjectRequest
        {
            Name = "new name",
            Description = "project description",
            MetaInfo = new()
            {
                Color = "blue",
            },
        };

        var response = await Client.PostAsJsonAsync("api/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddProjectRequest
        {
            Description = "project description",
            MetaInfo = new()
            {
                Color = "blue",
            },
        };

        var response = await Client.PostAsJsonAsync("api/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var project = await CreateProject();

        var response = await Client.DeleteAsync($"api/projects/{project.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/projects/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
    }

    private async Task<ProjectViewModel> CreateProject()
    {
        var response = await Client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            Name = $"Delete target {Guid.NewGuid():N}",
            Description = "Project created so the delete test owns its subject.",
            MetaInfo = new()
            {
                Color = "blue",
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        return result.Payload!;
    }
}
