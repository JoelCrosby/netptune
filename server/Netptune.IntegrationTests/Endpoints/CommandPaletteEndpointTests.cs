using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(UserPreferencesCollection.Name)]
public sealed class CommandPaletteEndpointTests
{
    private readonly NetptuneFixture Fixture;

    public CommandPaletteEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Recent_ShouldCreateAndUpsertWorkspaceRecentItems()
    {
        var client = Fixture.CreateNetptuneClient();
        await SetScope(client, "workspace");
        await ClearRecent(client);

        var url = $"/netptune/tasks/integration-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "first",
            title = "First title",
            url,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        created.IsSuccess.Should().BeTrue();
        created.Payload!.Scope.Should().Be("workspace");
        created.Payload.Items.Should().ContainSingle(item => item.Url == url && item.Title == "First title");

        var updateResponse = await client.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "second",
            title = "Updated title",
            url,
        });

        var updated = await updateResponse.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        updated.Payload!.Items.Should().ContainSingle(item =>
            item.Url == url &&
            item.Title == "Updated title" &&
            item.EntityId == "second");
    }

    [Fact]
    public async Task Recent_ShouldPruneWorkspaceItemsToTen()
    {
        var client = Fixture.CreateNetptuneClient();
        await SetScope(client, "workspace");
        await ClearRecent(client);

        var prefix = $"/netptune/tasks/prune-{Guid.NewGuid():N}";

        for (var i = 0; i < 12; i++)
        {
            await client.PostAsJsonAsync("api/command-palette/recent", new
            {
                type = "task",
                entityId = i.ToString(),
                title = $"Prune {i}",
                url = $"{prefix}-{i}",
            });
        }

        var result = await GetRecent(client);

        result.Scope.Should().Be("workspace");
        result.Items.Should().HaveCount(10);
        result.Items.Should().NotContain(item => item.Url == $"{prefix}-0");
        result.Items.Should().NotContain(item => item.Url == $"{prefix}-1");
    }

    [Fact]
    public async Task Recent_ShouldRespectGlobalScopeAcrossAccessibleWorkspaces()
    {
        var netptuneClient = Fixture.CreateNetptuneClient();
        var linuxClient = Fixture.CreateNetptuneClient();
        linuxClient.DefaultRequestHeaders.Remove("workspace");
        linuxClient.DefaultRequestHeaders.Add("workspace", "linux");

        await SetScope(netptuneClient, "global");
        await ClearRecent(netptuneClient);

        var suffix = Guid.NewGuid().ToString("N");
        var netptuneUrl = $"/netptune/tasks/global-{suffix}";
        var linuxUrl = $"/linux/tasks/global-{suffix}";

        await netptuneClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "netptune",
            title = "Netptune global recent",
            url = netptuneUrl,
        });

        await linuxClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "linux",
            title = "Linux global recent",
            url = linuxUrl,
        });

        var result = await GetRecent(netptuneClient);

        result.Scope.Should().Be("global");
        result.Items.Should().Contain(item => item.Url == netptuneUrl);
        result.Items.Should().Contain(item => item.Url == linuxUrl);
    }

    [Fact]
    public async Task Recent_ShouldKeepWorkspaceScopeIsolatedToCurrentWorkspace()
    {
        var netptuneClient = Fixture.CreateNetptuneClient();
        var linuxClient = Fixture.CreateNetptuneClient();
        linuxClient.DefaultRequestHeaders.Remove("workspace");
        linuxClient.DefaultRequestHeaders.Add("workspace", "linux");

        await SetScope(netptuneClient, "workspace");
        await SetScope(linuxClient, "workspace");
        await ClearRecent(netptuneClient);
        await ClearRecent(linuxClient);

        var suffix = Guid.NewGuid().ToString("N");
        var netptuneUrl = $"/netptune/tasks/workspace-{suffix}";
        var linuxUrl = $"/linux/tasks/workspace-{suffix}";

        await netptuneClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "netptune",
            title = "Netptune workspace recent",
            url = netptuneUrl,
        });

        await linuxClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "linux",
            title = "Linux workspace recent",
            url = linuxUrl,
        });

        var result = await GetRecent(netptuneClient);

        result.Scope.Should().Be("workspace");
        result.Items.Should().Contain(item => item.Url == netptuneUrl);
        result.Items.Should().NotContain(item => item.Url == linuxUrl);
    }

    [Fact]
    public async Task Recent_ShouldClearCurrentWorkspaceWhenScopeIsWorkspace()
    {
        var client = Fixture.CreateNetptuneClient();
        await SetScope(client, "workspace");
        await ClearRecent(client);

        await client.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "clear",
            title = "Clear me",
            url = $"/netptune/tasks/clear-{Guid.NewGuid():N}",
        });

        var clearResponse = await client.DeleteAsync("api/command-palette/recent");

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await clearResponse.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Recent_ShouldClearOnlyCurrentWorkspaceWhenScopeIsWorkspace()
    {
        var netptuneClient = Fixture.CreateNetptuneClient();
        var linuxClient = Fixture.CreateNetptuneClient();
        linuxClient.DefaultRequestHeaders.Remove("workspace");
        linuxClient.DefaultRequestHeaders.Add("workspace", "linux");

        await SetScope(netptuneClient, "workspace");
        await SetScope(linuxClient, "workspace");
        await ClearRecent(netptuneClient);
        await ClearRecent(linuxClient);

        var suffix = Guid.NewGuid().ToString("N");
        var netptuneUrl = $"/netptune/tasks/clear-workspace-{suffix}";
        var linuxUrl = $"/linux/tasks/clear-workspace-{suffix}";

        await netptuneClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "netptune",
            title = "Netptune clear workspace recent",
            url = netptuneUrl,
        });

        await linuxClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "linux",
            title = "Linux clear workspace recent",
            url = linuxUrl,
        });

        await netptuneClient.DeleteAsync("api/command-palette/recent");

        var linuxResult = await GetRecent(linuxClient);

        linuxResult.Items.Should().ContainSingle(item => item.Url == linuxUrl);
    }

    [Fact]
    public async Task Recent_ShouldClearAllUserRecentsWhenScopeIsGlobal()
    {
        var netptuneClient = Fixture.CreateNetptuneClient();
        var linuxClient = Fixture.CreateNetptuneClient();
        linuxClient.DefaultRequestHeaders.Remove("workspace");
        linuxClient.DefaultRequestHeaders.Add("workspace", "linux");

        await SetScope(netptuneClient, "global");
        await ClearRecent(netptuneClient);

        var suffix = Guid.NewGuid().ToString("N");
        var netptuneUrl = $"/netptune/tasks/clear-global-{suffix}";
        var linuxUrl = $"/linux/tasks/clear-global-{suffix}";

        await netptuneClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "netptune",
            title = "Netptune clear global recent",
            url = netptuneUrl,
        });

        await linuxClient.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            entityId = "linux",
            title = "Linux clear global recent",
            url = linuxUrl,
        });

        var clearResponse = await netptuneClient.DeleteAsync("api/command-palette/recent");

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await clearResponse.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Scope.Should().Be("global");
        result.Payload.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Recent_ShouldRejectInvalidCreateRequests()
    {
        var client = Fixture.CreateNetptuneClient();

        var response = await client.PostAsJsonAsync("api/command-palette/recent", new
        {
            type = "task",
            title = "",
            url = "/netptune/tasks/invalid",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        result.IsSuccess.Should().BeFalse();
    }

    private static async Task<CommandPaletteRecentItemsResponse> GetRecent(HttpClient client)
    {
        var response = await client.GetAsync("api/command-palette/recent");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommandPaletteRecentItemsResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private static async Task SetScope(HttpClient client, string scope)
    {
        var response = await client.PutAsJsonAsync(
            $"api/user-preferences/values/{PreferenceKeys.CommandPaletteRecentItemsScope}",
            new { scope = "workspace", value = scope });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task ClearRecent(HttpClient client)
    {
        var response = await client.DeleteAsync("api/command-palette/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
