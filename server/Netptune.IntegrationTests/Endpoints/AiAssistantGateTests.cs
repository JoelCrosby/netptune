using System.Net.Http.Json;

using FluentAssertions;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class AiAssistantGateTests
{
    private const string WorkspaceSlug = "netptune";

    private readonly NetptuneFixture Fixture;

    public AiAssistantGateTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task SendMessage_ShouldRefuse_WhenTheWorkspaceHasTheAssistantTurnedOff()
    {
        var client = Fixture.CreateNetptuneClient();

        await SetAssistantEnabled(client, false);

        try
        {
            var response = await client.PostAsJsonAsync(
                "api/ai/conversations/messages",
                new { text = "hello" });

            var body = await response.Content.ReadAsStringAsync();

            body.Should().Contain("turned off", "the workspace gate must stop the turn before any provider call");
        }
        finally
        {
            await SetAssistantEnabled(client, true);
        }
    }

    [Fact]
    public async Task ApplyChangeSet_ShouldRefuse_WhenTheWorkspaceHasTheAssistantTurnedOff()
    {
        var client = Fixture.CreateNetptuneClient();

        await SetAssistantEnabled(client, false);

        try
        {
            var response = await client.PostAsJsonAsync(
                $"api/ai/change-sets/{Guid.NewGuid()}/apply",
                new { changeIds = Array.Empty<long>() });

            response.IsSuccessStatusCode.Should().BeFalse();
        }
        finally
        {
            await SetAssistantEnabled(client, true);
        }
    }

    private static async Task SetAssistantEnabled(HttpClient client, bool enabled)
    {
        var response = await client.PutAsJsonAsync(
            "api/workspaces",
            new { slug = WorkspaceSlug, assistantEnabled = enabled });

        response.EnsureSuccessStatusCode();
    }
}
