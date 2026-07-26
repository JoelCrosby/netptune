using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Colors;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PublicWorkspaceEndpointTests
{
    private readonly HttpClient Client;

    public PublicWorkspaceEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetPublicWorkspace_ShouldReturnCorrectly_WhenWorkspaceIsPublic()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var response = await Client.GetAsync($"api/public/workspaces/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceViewModel>();

        result!.Slug.Should().Be(slug);
        result.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task GetPublicWorkspace_ShouldReturnNotFound_WhenWorkspaceIsPrivate()
    {
        var slug = $"private-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);

        var response = await Client.GetAsync($"api/public/workspaces/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicWorkspace_ShouldReturnNotFound_WhenWorkspaceDoesNotExist()
    {
        var response = await Client.GetAsync("api/public/workspaces/not-a-workspace-key");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task CreateWorkspace(string slug)
    {
        var response = await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetVisibility(string slug, bool isPublic)
    {
        var response = await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            IsPublic = isPublic,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<Workspace>>();

        result.IsSuccess.Should().BeTrue();
    }
}
