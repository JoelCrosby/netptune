using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class WorkspacesLeaveEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public WorkspacesLeaveEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Leave_ShouldReturnFailure_WhenUserIsOwner()
    {
        // The authenticated test user owns the seeded "netptune" workspace.
        var response = await Client.PostAsync("api/workspaces/netptune/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Leave_ShouldReturnNotFound_WhenWorkspaceDoesNotExist()
    {
        var response = await Client.PostAsync("api/workspaces/not-a-workspace-key/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Leave_ShouldReturnSuccess_WhenMemberLeaves()
    {
        // Seed a throwaway workspace owned by a different user so the test user
        // is a non-owner member that can leave without mutating shared state.
        const string slug = "leave-test-workspace";

        var (workspaceId, userId) = await SeedMembership(slug);

        var response = await Client.PostAsync($"api/workspaces/{slug}/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        (await IsMember(workspaceId, userId)).Should().BeFalse();
    }

    private async Task<(int WorkspaceId, string UserId)> SeedMembership(string slug)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        // The user the test client authenticates as (first member of "netptune").
        var userId = await context.WorkspaceAppUsers
            .Where(member => member.Workspace.Slug == "netptune")
            .Select(member => member.UserId)
            .FirstAsync();

        var ownerId = await context.AppUsers
            .Where(user => user.Id != userId)
            .Select(user => user.Id)
            .FirstAsync();

        var workspace = new Workspace
        {
            Name = "Leave Test Workspace",
            Slug = slug,
            OwnerId = ownerId,
            MetaInfo = new() { Color = "#cccccc" },
        };

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        context.WorkspaceAppUsers.Add(new WorkspaceAppUser
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = WorkspaceRole.Member,
            Permissions = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Member).ToList(),
        });

        await context.SaveChangesAsync();

        return (workspace.Id, userId);
    }

    private async Task<bool> IsMember(int workspaceId, string userId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.WorkspaceAppUsers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId);
    }
}
