using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Colors;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class WorkspacesEndpointTests
{
    private readonly HttpClient Client;

    public WorkspacesEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
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
                Color = NamedColors.FallbackColor,
            },
        };

        var response = await Client.PostAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<WorkspaceViewModel>>();

        result.IsSuccess.Should().BeTrue();
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
                Color = NamedColors.FallbackColor,
            },
        };

        var response = await Client.PostAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        const string slug = "linux";

        var previous = await Client.GetFromJsonAsync<WorkspaceViewModel>($"api/workspaces/{slug}");
        var request = new UpdateWorkspaceRequest
        {
            Name = "Arch Linux",
            Description = "Arch Linux test workspace",
            Slug = slug,
            MetaInfo = new ()
            {
                Color = NamedColors.FallbackColor,
            },
        };

        try
        {
            var response = await Client.PutAsJsonAsync("api/workspaces", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ClientResponse<UpdateWorkspaceResponse>>();

            result.IsSuccess.Should().BeTrue();
            result.Payload.Should().NotBeNull();
            result.Payload!.Workspace.Name.Should().Be(request.Name);
            result.Payload.Workspace.Description.Should().Be(request.Description);
        }
        finally
        {
            await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
            {
                Slug = slug,
                Name = previous!.Name,
                Description = previous.Description,
                MetaInfo = previous.MetaInfo,
            });
        }
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
                Color = NamedColors.FallbackColor,
            },
        };

        var response = await Client.PutAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenTheWorkspaceDoesNotExist()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "not-a-workspace-key",
            Name = "test workspace",
            MetaInfo = new () { Color = NamedColors.FallbackColor },
        };

        var response = await Client.PutAsJsonAsync("api/workspaces", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldRenameTheWorkspace_WhenNewSlugProvided()
    {
        var slug = $"rename-{Guid.NewGuid():N}"[..20];
        var renamed = $"{slug}-x";

        await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var response = await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            NewSlug = renamed,
            Name = "Renamed Workspace",
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<UpdateWorkspaceResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Workspace.Slug.Should().Be(renamed);
        result.Payload.PreviousSlug.Should().Be(slug);
        result.Payload.Workspace.Name.Should().Be("Renamed Workspace");

        (await Client.GetAsync($"api/workspaces/{renamed}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.GetAsync($"api/workspaces/{slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldAuthoriseUnderTheNewSlugImmediately_AfterARename()
    {
        var slug = $"authz-{Guid.NewGuid():N}"[..20];
        var renamed = $"{slug}-x";

        await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var beforeRename = await SendWithWorkspaceHeader("api/users", slug);

        beforeRename.Should().Be(HttpStatusCode.OK);

        await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            NewSlug = renamed,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var underNewSlug = await SendWithWorkspaceHeader("api/users", renamed);
        var underOldSlug = await SendWithWorkspaceHeader("api/users", slug);

        underNewSlug.Should().Be(HttpStatusCode.OK);
        underOldSlug.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpStatusCode> SendWithWorkspaceHeader(string url, string workspaceKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, url);

        message.Headers.Add("workspace", workspaceKey);

        var response = await Client.SendAsync(message);

        return response.StatusCode;
    }

    [Fact]
    public async Task Update_ShouldAuthoriseUnderTheNewSlug_WhenItWasProbedBeforeTheRename()
    {
        var slug = $"probe-{Guid.NewGuid():N}"[..20];
        var renamed = $"{slug}-x";

        await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var beforeRename = await SendWithWorkspaceHeader("api/users", renamed);

        beforeRename.Should().Be(HttpStatusCode.Forbidden);

        await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            NewSlug = renamed,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var afterRename = await SendWithWorkspaceHeader("api/users", renamed);

        afterRename.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenNewSlugIsAlreadyTaken()
    {
        var slug = $"taken-{Guid.NewGuid():N}"[..20];

        await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var response = await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            NewSlug = "netptune",
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await Client.GetAsync($"api/workspaces/{slug}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteTheWorkspace_WhenInputValid()
    {
        var slug = $"soft-delete-{Guid.NewGuid():N}"[..24];

        await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        var response = await Client.DeleteAsync($"api/workspaces/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        (await Client.GetAsync($"api/workspaces/{slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenCallerHasNoPermissionsInTheTargetWorkspace()
    {
        // Deletion authorises against the route key rather than the workspace header, so an
        // unknown key resolves to no permissions and never reaches the command.
        var response = await Client.DeleteAsync("api/workspaces/not-a-workspace-key");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/workspaces/is-unique/unique-workspace");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenInputIsNotUnique()
    {
        var response = await Client.GetAsync("api/workspaces/is-unique/netptune");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<IsSlugUniqueResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.IsUnique.Should().BeFalse();
    }
}
