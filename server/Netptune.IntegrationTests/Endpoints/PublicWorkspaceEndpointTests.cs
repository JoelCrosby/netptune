using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Colors;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pins;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Users;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Entities.Contexts;
using Netptune.Handlers.Pins.Commands;

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

    // Pins read as tasks.read, which is on the public allowlist, so an anonymous reader sees the
    // shared scopes. Nothing about a pin is personal to them, and nothing is theirs to remove.
    [Fact]
    public async Task AnonymousRequest_ShouldReturnSharedPinsOnly_WithoutAUser()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var owner = CreateOwnerClient(slug);
        var seed = await SeedWorkspace(slug);
        var task = await CreateTask(owner, seed);

        await Pin(owner, task.Id, TaskPinScope.Workspace);
        await Pin(owner, task.Id, TaskPinScope.User);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var response = await anonymous.GetAsync("api/pins");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pinned = await response.Content.ReadFromJsonAsync<List<PinnedTaskViewModel>>();
        var entry = pinned!.Should().ContainSingle(item => item.Task.Id == task.Id).Subject;

        entry.Pins.Select(pin => pin.Scope).Should().BeEquivalentTo([TaskPinScope.Workspace]);
        entry.Pins.Should().OnlyContain(pin => !pin.CanUnpin);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldReturnBoardPins_WithoutAUser()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var owner = CreateOwnerClient(slug);
        var seed = await SeedWorkspace(slug);
        var task = await CreateTask(owner, seed);

        await Pin(owner, task.Id, TaskPinScope.Board, seed.BoardId);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var pins = await anonymous.GetAsync($"api/pins/board/{seed.BoardId}");
        var boardView = await anonymous.GetAsync($"api/boards/view/{seed.BoardIdentifier}");

        pins.StatusCode.Should().Be(HttpStatusCode.OK);
        boardView.StatusCode.Should().Be(HttpStatusCode.OK, "the board view carries each card's pinned scopes");

        var pinned = await pins.Content.ReadFromJsonAsync<List<PinnedTaskViewModel>>();

        pinned!.Should().Contain(item => item.Task.Id == task.Id);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldBeDenied_ForPinWritesOnPublicWorkspace()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        await CreateWorkspace(slug);
        await SetVisibility(slug, isPublic: true);

        var owner = CreateOwnerClient(slug);
        var seed = await SeedWorkspace(slug);
        var task = await CreateTask(owner, seed);
        var pin = await Pin(owner, task.Id, TaskPinScope.Workspace);

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        var create = await anonymous.PostAsJsonAsync("api/pins", new CreateTaskPinRequest
        {
            TaskId = task.Id,
            Scope = TaskPinScope.User,
        });
        var reorder = await anonymous.PutAsJsonAsync("api/pins/reorder", new ReorderTaskPinsRequest
        {
            Items = [new TaskPinOrder(pin.Id, -1d)],
        });
        var delete = await anonymous.DeleteAsync($"api/pins/{pin.Id}");

        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        reorder.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateOwnerClient(string slug)
    {
        var client = Fixture.CreateNetptuneClient();

        client.DefaultRequestHeaders.Remove("workspace");
        client.DefaultRequestHeaders.Add("workspace", slug);

        return client;
    }

    // Every workspace is created with a project, a board, its groups and a status set, which is all a
    // task needs.
    private async Task<WorkspaceSeed> SeedWorkspace(string slug)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var workspace = await context.Workspaces.FirstAsync(item => item.Slug == slug, TestContext.Current.CancellationToken);
        var project = await context.Projects
            .FirstAsync(item => item.WorkspaceId == workspace.Id && !item.IsDeleted, TestContext.Current.CancellationToken);
        var board = await context.Boards
            .FirstAsync(item => item.WorkspaceId == workspace.Id && !item.IsDeleted, TestContext.Current.CancellationToken);
        var group = await context.BoardGroups
            .Where(item => item.BoardId == board.Id && !item.IsDeleted)
            .OrderBy(item => item.SortOrder)
            .FirstAsync(TestContext.Current.CancellationToken);
        var status = await context.Statuses
            .Where(item => item.WorkspaceId == workspace.Id && item.EntityType == EntityType.Task)
            .OrderBy(item => item.SortOrder)
            .FirstAsync(TestContext.Current.CancellationToken);

        return new WorkspaceSeed
        {
            ProjectId = project.Id,
            BoardId = board.Id,
            BoardIdentifier = board.Identifier,
            BoardGroupId = group.Id,
            StatusId = status.Id,
        };
    }

    private static async Task<TaskViewModel> CreateTask(HttpClient owner, WorkspaceSeed seed)
    {
        var response = await owner.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = $"Public pin {Guid.NewGuid():N}",
            Description = "Created by the public workspace integration tests",
            StatusId = seed.StatusId,
            ProjectId = seed.ProjectId,
            BoardGroupId = seed.BoardGroupId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return result.Payload!;
    }

    private static async Task<TaskPinViewModel> Pin(HttpClient owner, int taskId, TaskPinScope scope, int? scopeEntityId = null)
    {
        var response = await owner.PostAsJsonAsync("api/pins", new CreateTaskPinRequest
        {
            TaskId = taskId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskPinViewModel>>();

        return result.Payload!;
    }

    private sealed record WorkspaceSeed
    {
        public required int ProjectId { get; init; }

        public required int BoardId { get; init; }

        public required string BoardIdentifier { get; init; }

        public required int BoardGroupId { get; init; }

        public required int StatusId { get; init; }
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
