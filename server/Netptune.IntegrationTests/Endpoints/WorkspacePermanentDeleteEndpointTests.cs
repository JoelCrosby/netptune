using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

/// <summary>
/// Permanent workspace deletion has to clear every row pointing at the workspace before the
/// workspace row itself can go — all of those foreign keys are <c>Restrict</c>. These run against
/// real Postgres, which is the only thing that actually enforces that.
/// </summary>
[Collection(WorkspaceMutationCollection.Name)]
public sealed class WorkspacePermanentDeleteEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public WorkspacePermanentDeleteEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task DeletePermanent_ShouldRemoveTheWorkspace_AndEverythingHangingOffIt()
    {
        var slug = "permanent-delete-test";

        var workspace = await CreateWorkspace(slug);

        // A freshly created workspace is not empty: WorkspaceFactory seeds statuses, relation types,
        // a default project and an owner membership row. All of those hold the workspace down.
        var workspaceId = workspace.Id;

        await AssertWorkspaceRowsExist(workspaceId);

        var response = await Client.DeleteAsync($"api/workspaces/permanent/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        await AssertWorkspaceFullyGone(workspaceId);
    }

    private async Task<WorkspaceViewModel> CreateWorkspace(string slug)
    {
        var response = await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = "#cccccc" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<WorkspaceViewModel>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task AssertWorkspaceRowsExist(int workspaceId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        (await context.Statuses.CountAsync(x => x.WorkspaceId == workspaceId)).Should().BePositive();
        (await context.RelationTypes.CountAsync(x => x.WorkspaceId == workspaceId)).Should().BePositive();
        (await context.Projects.CountAsync(x => x.WorkspaceId == workspaceId)).Should().BePositive();
        (await context.WorkspaceAppUsers.CountAsync(x => x.WorkspaceId == workspaceId)).Should().BePositive();
    }

    private async Task AssertWorkspaceFullyGone(int workspaceId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        (await context.Workspaces.AnyAsync(x => x.Id == workspaceId)).Should().BeFalse();

        (await context.Statuses.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.RelationTypes.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Sprints.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Flags.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.AutomationRules.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.EventRecords.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.ActivityEntries.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Projects.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.ProjectTasks.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Boards.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.BoardGroups.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Tags.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.Comments.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.WorkspaceAppUsers.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.WorkspaceInvites.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
        (await context.ProjectTaskRelations.AnyAsync(x => x.WorkspaceId == workspaceId)).Should().BeFalse();
    }
}
