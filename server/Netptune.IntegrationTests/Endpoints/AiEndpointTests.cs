using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Ai;

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
            model = "gpt-5.2",
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

    private static async Task DeleteExistingCredentials(HttpClient client)
    {
        var existing = await client.GetFromJsonAsync<List<AiCredentialViewModel>>("api/ai/credentials");

        foreach (var credential in existing ?? [])
        {
            await client.DeleteAsync($"api/ai/credentials/{credential.Id}");
        }
    }
}
