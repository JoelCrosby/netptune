using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(Collections.Database)]
public sealed class WorkspacesEndpointTests
{
    private readonly HttpClient Client;

    public WorkspacesEndpointTests(NetptuneApiFactory factory)
    {
        Client = factory.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<Workspace>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByKey_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/workspaces/netptune");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Workspace>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByKey_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/workspaces/not-a-workspace-key");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/workspaces/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<Workspace>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddWorkspaceRequest
        {
            Name = "create test workspace",
            Description = "create test workspace description",
            Slug = "create-test-workspace",
            MetaInfo = new ()
            {
                Color = "#cccccc",
            },
        };

        var response = await Client.PostAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<WorkspaceViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddWorkspaceRequest
        {
            Description = "create test workspace description",
            Slug = "create-test-workspace",
            MetaInfo = new ()
            {
                Color = "#cccccc",
            },
        };

        var response = await Client.PostAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new UpdateWorkspaceRequest
        {
            Name = "Arch Linux",
            Description = "Arch Linux test workspace",
            Slug = "linux",
            MetaInfo = new ()
            {
                Color = "#cccccc",
            },
        };

        var response = await Client.PutAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<WorkspaceViewModel>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new UpdateWorkspaceRequest
        {
            Name = "test workspace",
            Description = "create test workspace description",
            MetaInfo = new ()
            {
                Color = "#cccccc",
            },
        };

        var response = await Client.PostAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/workspaces/is-unique/unique-workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenInputIsNotUnique()
    {
        var response = await Client.GetAsync("api/workspaces/is-unique/netptune");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result!.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.IsUnique.Should().BeFalse();
    }
}
