using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Projects;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class WorkspaceBroadcastEndpointTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;
    private readonly string SourceClientId = Guid.NewGuid().ToString("N");

    public WorkspaceBroadcastEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
        Client.DefaultRequestHeaders.Add(WorkspaceEventProbe.RealtimeClientHeader, SourceClientId);
    }

    [Fact]
    public async Task CreateStatus_ShouldBroadcastAStatusEvent()
    {
        var published = await Published(WorkspaceEventScopes.Status, () =>
        {
            return Client.PostAsJsonAsync("api/statuses", new CreateStatusRequest
            {
                Name = $"Broadcast {Guid.NewGuid():N}",
                Category = StatusCategory.Todo,
            });
        });

        published.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTag_ShouldBroadcastATagEvent()
    {
        var published = await Published(WorkspaceEventScopes.Tag, () =>
        {
            return Client.PostAsJsonAsync("api/tags", new AddTagRequest { Tag = $"broadcast-{Guid.NewGuid():N}" });
        });

        published.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProject_ShouldBroadcastAProjectEvent()
    {
        var published = await Published(WorkspaceEventScopes.Project, () =>
        {
            return Client.PostAsJsonAsync("api/projects", NewProject());
        });

        published.Should().BeTrue();
    }

    [Fact]
    public async Task AddComment_ShouldBroadcastACommentEvent()
    {
        var published = await Published(WorkspaceEventScopes.Comment, () =>
        {
            return Client.PostAsJsonAsync("api/comments/task", new AddCommentRequest
            {
                Comment = "Broadcast comment",
                SystemId = "neo-1",
            });
        });

        published.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSprint_ShouldBroadcastASprintEvent()
    {
        var projectResponse = await Client.PostAsJsonAsync("api/projects", NewProject());
        var project = await projectResponse.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        var published = await Published(WorkspaceEventScopes.Sprint, () =>
        {
            return Client.PostAsJsonAsync("api/sprints", new AddSprintRequest
            {
                Name = $"Broadcast {Guid.NewGuid():N}",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14),
                ProjectId = project.Payload!.Id,
            });
        });

        published.Should().BeTrue();
    }

    private Task<bool> Published(string scope, Func<Task<HttpResponseMessage>> request)
    {
        return WorkspaceEventProbe.Published(Fixture.Services, SourceClientId, scope, async () =>
        {
            var response = await request();

            response.EnsureSuccessStatusCode();
        });
    }

    private static AddProjectRequest NewProject()
    {
        return new AddProjectRequest
        {
            Name = $"{Guid.NewGuid():N} Broadcast",
            Description = "Project for broadcast integration tests",
            MetaInfo = new() { Color = "blue" },
        };
    }
}
