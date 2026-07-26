using System.Net;

using FluentAssertions;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class RealtimeHubEndpointTests
{
    private readonly HttpClient Client;

    public RealtimeHubEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task NotificationsHub_ShouldOpenAnEventStream_WhenAuthenticated()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var response = await Client.GetAsync(
            "api/hubs/notifications",
            HttpCompletionOption.ResponseHeadersRead,
            cancellation.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        await cancellation.CancelAsync();
    }

    [Fact]
    public async Task BoardEventsHub_ShouldOpenAnEventStream_WhenWorkspacePermissionsResolve()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var response = await Client.GetAsync(
            $"api/hubs/board-events?workspace=netptune&group=board-1&clientId={Guid.NewGuid():N}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellation.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        await cancellation.CancelAsync();
    }

    [Fact]
    public async Task BoardEventsHub_ShouldReturnForbidden_WhenUserHasNoPermissionsInWorkspace()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var response = await Client.GetAsync(
            $"api/hubs/board-events?workspace=not-a-workspace-key&group=board-1&clientId={Guid.NewGuid():N}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellation.Token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
