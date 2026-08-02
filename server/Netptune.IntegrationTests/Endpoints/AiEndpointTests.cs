using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Ai;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AiEndpointTests
{
    private readonly NetptuneFixture Fixture;

    public AiEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Credentials_ShouldRoundTripWithoutExposingTheSecret()
    {
        var client = Fixture.CreateNetptuneClient();

        await DeleteExistingCredentials(client);

        var saveResponse = await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-integration-secret-value",
        });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await saveResponse.Content.ReadFromJsonAsync<ClientResponse<AiCredentialViewModel>>();

        saved.IsSuccess.Should().BeTrue();
        saved.Payload!.SecretHint.Should().Be("alue");

        var listed = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        listed.Should().ContainSingle(credential => credential.Provider == AiProvider.Anthropic);

        var payload = await client.GetStringAsync("api/ai/credentials");

        payload.Should().NotContain("sk-ant-integration-secret-value");

        await DeleteExistingCredentials(client);
    }

    [Fact]
    public async Task Credentials_ShouldReplaceTheStoredKey_WhenSavedTwiceForOneProvider()
    {
        var client = Fixture.CreateNetptuneClient();

        await DeleteExistingCredentials(client);

        await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "First",
            secret = "sk-ant-first-secret-aaaa",
        });

        await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Second",
            secret = "sk-ant-second-secret-bbbb",
        });

        var listed = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        listed.Should().ContainSingle();
        listed![0].Label.Should().Be("Second");
        listed[0].SecretHint.Should().Be("bbbb");

        await DeleteExistingCredentials(client);
    }

    [Fact]
    public async Task Credentials_ShouldStoreThePerKeyModelOverride()
    {
        var client = Fixture.CreateNetptuneClient();

        await DeleteExistingCredentials(client);

        await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-model-override-secret",
            model = "claude-sonnet-5",
        });

        var listed = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        listed.Should().ContainSingle();
        listed![0].Model.Should().Be("claude-sonnet-5");

        await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-model-override-secret",
            model = (string?)null,
        });

        var cleared = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        cleared![0].Model.Should().BeNull("clearing the field must fall back to the configured default");

        await DeleteExistingCredentials(client);
    }

    [Fact]
    public async Task Credentials_ShouldRejectAModelThatIsNotInTheCatalogue()
    {
        var client = Fixture.CreateNetptuneClient();

        var response = await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-catalogue-check-secret",
            model = "gpt-5.6-sol",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Models_ShouldListTheCatalogue()
    {
        var client = Fixture.CreateNetptuneClient();
        var models = await client.GetFromJsonAsync<List<AiModelOption>>("api/ai/models");

        models.Should().NotBeNullOrEmpty();
        models.Should().Contain(model => model.Id == AiModels.AnthropicDefault && model.IsDefault);
        models.Should().Contain(model => model.Provider == AiProvider.OpenAi);
    }

    [Fact]
    public async Task Credentials_ShouldRejectAShortSecret()
    {
        var client = Fixture.CreateNetptuneClient();

        var response = await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "short",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Conversations_ShouldReturnNotFound_WhenTheConversationDoesNotExist()
    {
        var client = Fixture.CreateNetptuneClient();
        var response = await client.GetAsync($"api/ai/conversations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeSets_ShouldReturnNotFound_WhenTheChangeSetDoesNotExist()
    {
        var client = Fixture.CreateNetptuneClient();
        var response = await client.GetAsync($"api/ai/change-sets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeSets_ShouldReturnNotFound_WhenDiscardingAnUnknownChangeSet()
    {
        var client = Fixture.CreateNetptuneClient();
        var response = await client.PostAsync($"api/ai/change-sets/{Guid.NewGuid()}/discard", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminConversations_ShouldListForAWorkspaceAdministrator()
    {
        var client = Fixture.CreateNetptuneClient();
        var conversations = await client
            .GetFromJsonAsync<List<AiWorkspaceConversationViewModel>>("api/ai/admin/conversations");

        conversations.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminConversations_ShouldReturnNotFound_WhenTheConversationDoesNotExist()
    {
        var client = Fixture.CreateNetptuneClient();
        var response = await client.GetAsync($"api/ai/admin/conversations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Conversations_ShouldListWithoutError()
    {
        var client = Fixture.CreateNetptuneClient();
        var conversations = await client.GetFromJsonAsync<List<AiConversationViewModel>>("api/ai/conversations");

        conversations.Should().NotBeNull();
    }

    [Fact]
    public async Task Conversations_ShouldReturnThePendingChangeSet_SoAReloadCanRestoreIt()
    {
        var client = Fixture.CreateNetptuneClient();
        var seed = await SeedPendingChangeSet();

        try
        {
            var detail = await client.GetFromJsonAsync<ClientResponse<AiConversationDetailViewModel>>(
                $"api/ai/conversations/{seed.ConversationId}");

            detail.IsSuccess.Should().BeTrue();

            var pending = detail.Payload!.PendingChangeSet;

            pending.Should().NotBeNull("a reload has no other way to recover an unapplied proposal");
            pending!.Id.Should().Be(seed.ChangeSetId);
            pending.Status.Should().Be(AiChangeSetStatus.Pending);
            pending.Changes.Should().ContainSingle(change => change.ToolName == "propose_update_sprint");
        }
        finally
        {
            await RemoveSeed(seed.ConversationId);
        }
    }

    [Fact]
    public async Task PendingChangeSet_ShouldBeReadableByConversation_SoALostProposalEventCanRecover()
    {
        var client = Fixture.CreateNetptuneClient();
        var seed = await SeedPendingChangeSet();

        try
        {
            var response = await client.GetFromJsonAsync<ClientResponse<AiChangeSetViewModel>>(
                $"api/ai/conversations/{seed.ConversationId}/change-set");

            response.IsSuccess.Should().BeTrue();
            response.Payload!.Id.Should().Be(seed.ChangeSetId);
            response.Payload.Status.Should().Be(AiChangeSetStatus.Pending);
        }
        finally
        {
            await RemoveSeed(seed.ConversationId);
        }
    }

    [Fact]
    public async Task PendingChangeSet_ShouldReturnNotFound_WhenTheConversationHasNoProposal()
    {
        var client = Fixture.CreateNetptuneClient();

        var response = await client.GetAsync($"api/ai/conversations/{Guid.NewGuid()}/change-set");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMessage_ShouldStoreTheProposal_WhenTheClientDisconnectsMidTurn()
    {
        var client = Fixture.CreateNetptuneClient();
        var taskId = await ReadTaskId();

        await SaveCredential(client);
        ScriptTaskProposal(taskId);

        var conversationId = await StartTurnThenDisconnect(client);

        try
        {
            var changeSet = await WaitForPendingChangeSet(conversationId);

            changeSet.Should().NotBeNull(
                "the turn finishes server side even when nothing is left to stream the proposal to");
            changeSet!.Changes.Should().ContainSingle(change => change.ToolName == "propose_update_task");

            var recovered = await client.GetFromJsonAsync<ClientResponse<AiChangeSetViewModel>>(
                $"api/ai/conversations/{conversationId}/change-set");

            recovered.IsSuccess.Should().BeTrue("the client recovers the proposal through this endpoint");
            recovered.Payload!.Id.Should().Be(changeSet.Id);
        }
        finally
        {
            await RemoveSeed(conversationId);
            await DeleteExistingCredentials(client);
            ResetScript();
        }
    }

    private async Task<Guid> StartTurnThenDisconnect(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/conversations/messages")
        {
            Content = JsonContent.Create(new { text = "Rename the sprint please." }),
        };

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);

        var line = await reader.ReadLineAsync(TestContext.Current.CancellationToken);

        line.Should().NotBeNull().And.StartWith("data: ");

        var payload = JsonDocument.Parse(line![6..]);

        return payload.RootElement.GetProperty("conversationId").GetGuid();
    }

    private async Task<AiChangeSetViewModel?> WaitForPendingChangeSet(Guid conversationId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);

            using var scope = Fixture.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var changeSet = await context.AiChangeSets
                .Where(item => item.ConversationId == conversationId)
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

            if (changeSet is null)
            {
                continue;
            }

            var changes = await context.AiProposedChanges
                .Where(item => item.ChangeSetId == changeSet.Id)
                .ToListAsync(TestContext.Current.CancellationToken);

            return new AiChangeSetViewModel
            {
                Id = changeSet.Id,
                ConversationId = changeSet.ConversationId,
                Status = changeSet.Status,
                Changes = changes.Select(change => new AiProposedChangeViewModel
                {
                    Id = change.Id,
                    Sequence = change.Sequence,
                    ToolName = change.ToolName,
                    EntityType = change.EntityType,
                    Summary = change.Summary,
                }).ToList(),
            };
        }

        return null;
    }

    private async Task<int> ReadTaskId()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var task = await context.ProjectTasks
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        return task.Id;
    }

    private void ScriptTaskProposal(int taskId)
    {
        using var scope = Fixture.CreateScope();
        var script = scope.ServiceProvider.GetRequiredService<TestAiChatScript>();

        script.Reset();
        script.DelayBeforeCompletion = TimeSpan.FromMilliseconds(400);
        script.Enqueue(new AiChatTurn
        {
            Text = string.Empty,
            ToolCalls =
            [
                new AiToolCall
                {
                    Id = "call-1",
                    Name = "propose_update_task",
                    Arguments = JsonDocument.Parse(
                        $$"""{"taskId":{{taskId}},"priority":"High"}"""),
                },
            ],
        });
    }

    private void ResetScript()
    {
        using var scope = Fixture.CreateScope();

        scope.ServiceProvider.GetRequiredService<TestAiChatScript>().Reset();
    }

    private static async Task SaveCredential(HttpClient client)
    {
        var response = await client.PutAsJsonAsync("api/ai/credentials", new
        {
            provider = AiProvider.Anthropic,
            label = "Anthropic",
            secret = "sk-ant-integration-secret-value",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record PendingChangeSetSeed(Guid ConversationId, Guid ChangeSetId);

    private async Task<PendingChangeSetSeed> SeedPendingChangeSet()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var membership = await context.WorkspaceAppUsers
            .Include(workspaceUser => workspaceUser.Workspace)
            .Include(workspaceUser => workspaceUser.User)
            .Where(workspaceUser =>
                workspaceUser.Workspace.Slug == "netptune" &&
                workspaceUser.User.UserType == AppUserType.User)
            .FirstAsync(TestContext.Current.CancellationToken);

        var conversation = new AiConversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = membership.WorkspaceId,
            UserId = membership.UserId,
            Title = "Reload safety",
            Provider = AiProvider.Anthropic,
            Model = "claude-opus-5",
            LastMessageAt = DateTime.UtcNow,
        };

        var message = new AiMessage
        {
            ConversationId = conversation.Id,
            Sequence = 0,
            Role = AiMessageRole.Assistant,
            Model = conversation.Model,
            Provider = conversation.Provider,
            Content = new AiMessageContent { Text = "Here is what I propose." }.ToJsonDocument(),
            CreatedAt = DateTime.UtcNow,
        };

        await context.AiConversations.AddAsync(conversation, TestContext.Current.CancellationToken);
        await context.AiMessages.AddAsync(message, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var changeSet = new AiChangeSet
        {
            Id = Guid.NewGuid(),
            WorkspaceId = membership.WorkspaceId,
            ConversationId = conversation.Id,
            MessageId = message.Id,
            UserId = membership.UserId,
            Status = AiChangeSetStatus.Pending,
            CorrelationId = Guid.NewGuid(),
        };

        var change = new AiProposedChange
        {
            ChangeSetId = changeSet.Id,
            Sequence = 0,
            ToolName = "propose_update_sprint",
            EntityType = "sprint",
            Summary = "Update name on sprint “Sprint 4”",
        };

        await context.AiChangeSets.AddAsync(changeSet, TestContext.Current.CancellationToken);
        await context.AiProposedChanges.AddAsync(change, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new PendingChangeSetSeed(conversation.Id, changeSet.Id);
    }

    private async Task RemoveSeed(Guid conversationId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var conversation = await context.AiConversations
            .FirstOrDefaultAsync(item => item.Id == conversationId, TestContext.Current.CancellationToken);

        if (conversation is null)
        {
            return;
        }

        var changeSets = await context.AiChangeSets
            .Where(item => item.ConversationId == conversationId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var changeSetIds = changeSets.Select(item => item.Id).ToList();
        var changes = await context.AiProposedChanges
            .Where(item => changeSetIds.Contains(item.ChangeSetId))
            .ToListAsync(TestContext.Current.CancellationToken);

        var messages = await context.AiMessages
            .Where(item => item.ConversationId == conversationId)
            .ToListAsync(TestContext.Current.CancellationToken);

        context.AiProposedChanges.RemoveRange(changes);
        context.AiChangeSets.RemoveRange(changeSets);
        context.AiMessages.RemoveRange(messages);
        context.AiConversations.Remove(conversation);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task DeleteExistingCredentials(HttpClient client)
    {
        var existing = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        foreach (var credential in existing ?? [])
        {
            await client.DeleteAsync($"api/ai/credentials/{credential.Id}");
        }
    }
}
