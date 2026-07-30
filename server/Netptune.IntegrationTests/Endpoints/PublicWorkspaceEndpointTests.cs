using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Colors;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;
using Netptune.Core.ViewModels.Workspace;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PublicWorkspaceEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public PublicWorkspaceEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
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

        var result = await response.Content.ReadFromJsonAsync<PublicWorkspaceViewModel>();

        result!.Slug.Should().Be(slug);
        result.IsPublic.Should().BeTrue();
        result.PublicPermissions.Should().BeEquivalentTo(NetptunePermissions.PublicReadable);
    }

    [Fact]
    public void PublicReadAllowlist_ShouldOnlyContainSafeReadPermissions()
    {
        NetptunePermissions.PublicReadable.Should().OnlyContain(permission => permission.EndsWith(".read"));

        NetptunePermissions.PublicReadable.Should().NotContain([
            NetptunePermissions.Members.Read,
            NetptunePermissions.Comments.Read,
            NetptunePermissions.Activity.Read,
            NetptunePermissions.Audit.Read,
            NetptunePermissions.Notifications.Read,
            NetptunePermissions.Storage.Read,
            NetptunePermissions.Files.Read,
            NetptunePermissions.Workspace.Read,
            NetptunePermissions.Automations.Read,
            NetptunePermissions.ServiceAccounts.Read,
            NetptunePermissions.Flags.Read,
        ]);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldBeAllowed_ForAllowlistedReadOnPublicWorkspace()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync("api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("api/users")]
    [InlineData("api/audit")]
    [InlineData("api/notifications")]
    [InlineData("api/automations")]
    [InlineData("api/user-preferences/values")]
    public async Task AnonymousRequest_ShouldBeDenied_ForEndpointsOutsideThePublicAllowlist(string route)
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldRespectTheWorkspacesPublicPermissionSelection()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);
        await SetPublicPermissions(slug, [NetptunePermissions.Projects.Read]);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var projects = await anonymous.GetAsync("api/projects");
        var sprints = await anonymous.GetAsync("api/sprints");

        projects.StatusCode.Should().Be(HttpStatusCode.OK);
        sprints.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldBeDenied_WhenTheWorkspaceExposesNothing()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);
        await SetPublicPermissions(slug, []);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync("api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetPublicPermissions_ShouldDropAnythingOutsideTheCeiling()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);
        await SetPublicPermissions(slug, [
            NetptunePermissions.Tasks.Read,
            NetptunePermissions.Members.Read,
            NetptunePermissions.Audit.Read,
            NetptunePermissions.Tasks.Delete,
        ]);

        var response = await Client.GetAsync($"api/public/workspaces/{slug}");
        var result = await response.Content.ReadFromJsonAsync<PublicWorkspaceViewModel>();

        result!.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        (await anonymous.GetAsync("api/users")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("api/audit")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetPublicPermissions_ShouldSurviveAVisibilityRoundTrip()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);
        await SetPublicPermissions(slug, [NetptunePermissions.Tasks.Read]);

        await SetVisibility(slug, isPublic: false);
        await SetVisibility(slug, isPublic: true);

        var response = await Client.GetAsync($"api/public/workspaces/{slug}");
        var result = await response.Content.ReadFromJsonAsync<PublicWorkspaceViewModel>();

        result!.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldReturnCurrentSprint_WithoutAUser()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync("api/sprints/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldBeDenied_ForWritesOnPublicWorkspace()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            Name = "anonymous project",
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldBeDenied_ForPrivateWorkspace()
    {
        var slug = $"private-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync("api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPublicWorkspaceMembers_ShouldReturnIdentitiesWithoutEmails()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync($"api/public/workspaces/{slug}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<AssigneeViewModel>>();

        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(member => !string.IsNullOrWhiteSpace(member.DisplayName));

        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContainEquivalentOf(
            "email",
            "the public member projection must not carry email addresses");
    }

    [Fact]
    public async Task GetPublicWorkspaceMembers_ShouldReturnNotFound_WhenWorkspaceIsPrivate()
    {
        var slug = $"private-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);

        var response = await Client.GetAsync($"api/public/workspaces/{slug}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task SetPublicPermissions(string slug, List<string> permissions)
    {
        var response = await Client.PutAsJsonAsync("api/workspaces", new UpdateWorkspaceRequest
        {
            Slug = slug,
            PublicPermissions = permissions,
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
