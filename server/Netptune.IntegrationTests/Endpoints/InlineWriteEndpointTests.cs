using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

// Saves an assistant key for the seeded user, which every other assistant test reads.
[Collection(WorkspaceMutationCollection.Name)]
public sealed class InlineWriteEndpointTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public InlineWriteEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Write_ShouldBeForbidden_WithoutAssistantUse()
    {
        var systemId = await AnyLiveTaskSystemId();
        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Assistant.Use);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var request = new InlineWriteRequest { Prompt = "Add a checklist" };
        var response = await memberClient.PostAsJsonAsync(
            $"api/ai/tasks/{systemId}/description",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Write_ShouldStreamTheReply_ForATaskInTheWorkspace()
    {
        var systemId = await AnyLiveTaskSystemId();

        await SaveCredential();

        try
        {
            Script().Enqueue(new AiChatTurn { Text = "- [ ] Ship it", Usage = new AiUsage { OutputTokens = 5 } });

            var request = new InlineWriteRequest { Prompt = "Add a checklist", Before = "Intro." };
            var frames = await Write(systemId, request);

            frames.Select(frame => frame.GetProperty("type").GetInt32()).Should().Equal(
                (int)AiStreamEventType.TextDelta,
                (int)AiStreamEventType.TurnCompleted);
            frames[0].GetProperty("text").GetString().Should().Be("- [ ] Ship it");

            var sent = Script().Requests.Should().ContainSingle().Subject;

            sent.Messages.Single().Text.Should().Contain("<before-cursor>\nIntro.\n</before-cursor>");
        }
        finally
        {
            await DeleteCredentials();
            Script().Reset();
        }
    }

    [Fact]
    public async Task Write_ShouldStreamAnError_ForAnUnknownTask()
    {
        var frames = await Write("NOPE-999999", new InlineWriteRequest { Prompt = "Anything" });

        frames.Should().ContainSingle().Which.GetProperty("type").GetInt32().Should().Be((int)AiStreamEventType.Error);
    }

    [Fact]
    public async Task Write_ShouldBeForbidden_WithoutTasksRead()
    {
        var systemId = await AnyLiveTaskSystemId();
        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Tasks.Read);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var request = new InlineWriteRequest { Prompt = "Add a checklist" };
        var response = await memberClient.PostAsJsonAsync(
            $"api/ai/tasks/{systemId}/description",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WritePage_ShouldBeForbidden_WithoutPagesUpdate()
    {
        var page = await CreatePage();
        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Pages.Update);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var request = new InlineWriteRequest { Prompt = "Add a rollback section" };
        var response = await memberClient.PostAsJsonAsync(
            $"api/ai/pages/{page.Id}/write",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WritePage_ShouldStreamTheReply_ForAPageInTheWorkspace()
    {
        var page = await CreatePage();

        await SaveCredential();

        try
        {
            Script().Enqueue(new AiChatTurn { Text = "## Rollback", Usage = new AiUsage { OutputTokens = 3 } });

            var request = new InlineWriteRequest { Prompt = "Add a rollback section" };
            var frames = await Post($"api/ai/pages/{page.Id}/write", request);

            frames.Select(frame => frame.GetProperty("type").GetInt32()).Should().Equal(
                (int)AiStreamEventType.TextDelta,
                (int)AiStreamEventType.TurnCompleted);

            var sent = Script().Requests.Should().ContainSingle().Subject;

            sent.Messages.Single().Text.Should().Contain($"Title: {page.Title}");
        }
        finally
        {
            await DeleteCredentials();
            Script().Reset();
        }
    }

    private Task<List<JsonElement>> Write(string systemId, InlineWriteRequest request)
    {
        return Post($"api/ai/tasks/{systemId}/description", request);
    }

    private async Task<List<JsonElement>> Post(string url, InlineWriteRequest request)
    {
        var response = await Client.PostAsJsonAsync(url, request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return stream
            .Split('\n')
            .Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonDocument.Parse(line[6..]).RootElement)
            .ToList();
    }

    private async Task<PageViewModel> CreatePage()
    {
        var create = new CreatePageRequest { Kind = PageKind.Document, Title = $"Inline write {Guid.NewGuid():N}" };
        var response = await Client.PostAsJsonAsync("api/pages", create, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }

    private TestAiChatScript Script()
    {
        using var scope = Fixture.CreateScope();

        return scope.ServiceProvider.GetRequiredService<TestAiChatScript>();
    }

    private async Task<string> AnyLiveTaskSystemId()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks
            .AsNoTracking()
            .Include(item => item.Project)
            .Where(item => item.WorkspaceId == 1 && !item.IsDeleted && !item.Project!.IsDeleted)
            .OrderBy(item => item.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        return $"{task.Project!.Key}-{task.ProjectScopeId}";
    }

    private async Task SaveCredential()
    {
        var response = await Client.PutAsJsonAsync(
            "api/ai/credentials",
            new { provider = AiProvider.Anthropic, label = "Anthropic", secret = "sk-ant-integration-secret-value" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task DeleteCredentials()
    {
        var existing = await Client.GetFromJsonAsync<List<AiCredentialViewModel>>(
            "api/ai/credentials",
            TestContext.Current.CancellationToken);

        foreach (var credential in existing ?? [])
        {
            await Client.DeleteAsync($"api/ai/credentials/{credential.Id}", TestContext.Current.CancellationToken);
        }
    }
}
