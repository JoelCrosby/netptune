using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Tags;

using StackExchange.Redis;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ApiV1PagesEndpointTests
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);

    private static string? ApiKey;

    private static readonly string[] Permissions =
    [
        NetptunePermissions.Pages.Read,
        NetptunePermissions.Pages.Create,
        NetptunePermissions.Pages.Update,
        NetptunePermissions.Pages.Delete,
        NetptunePermissions.Tags.Read,
        NetptunePermissions.Tags.Create,
        NetptunePermissions.Tags.Assign,
    ];

    private readonly NetptuneFixture Fixture;

    public ApiV1PagesEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task CreatePage_ShouldReturnCreated_AndBeReadableByIdAndSlug()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("api/v1/pages", new CreatePageRequest
        {
            Title = $"API page {Guid.NewGuid():N}",
            Body = "# Hello\n\nWritten through the public API.",
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(cancellationToken));
        response.Headers.Location!.ToString().Should().MatchRegex(@"^/api/v1/pages/\d+$");

        var created = await Read<PageViewModel>(response);

        created.Kind.Should().Be(PageKind.Document);
        created.Revision.Should().Be(1);

        var byId = await client.GetFromJsonAsync<PageViewModel>($"api/v1/pages/{created.Id}", cancellationToken);

        byId!.Body.Should().Be("# Hello\n\nWritten through the public API.");
        byId.Contributors.Should().NotBeEmpty();

        var bySlug = await client.GetFromJsonAsync<PageViewModel>($"api/v1/pages/by-slug/{created.Slug}", cancellationToken);

        bySlug!.Id.Should().Be(created.Id);

        var tree = await client.GetFromJsonAsync<List<PageTreeNodeViewModel>>("api/v1/pages", cancellationToken);

        tree.Should().Contain(node => node.Id == created.Id);
    }

    [Fact]
    public async Task GetPage_ShouldReturnNotFound_WhenThePageDoesNotExist()
    {
        var client = await CreateClient();

        var response = await client.GetAsync("api/v1/pages/999999999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SavePageContent_ShouldSaveARevision_AndResolveTheOldSlug()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Draft title");

        var response = await client.PutAsJsonAsync($"api/v1/pages/{page.Id}/content", new SavePageRequest
        {
            Title = "Renamed title",
            Body = "Updated body",
            Revision = page.Revision,
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var saved = await Read<PageViewModel>(response);

        saved.Title.Should().Be("Renamed title");
        saved.Body.Should().Be("Updated body");
        saved.Revision.Should().Be(page.Revision + 1);

        var byOldSlug = await client.GetFromJsonAsync<PageViewModel>($"api/v1/pages/by-slug/{page.Slug}", cancellationToken);

        byOldSlug!.Id.Should().Be(page.Id);
    }

    [Fact]
    public async Task SavePageContent_ShouldReturnConflictWithTheCurrentPage_WhenTheRevisionIsStale()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Contested page");

        await SaveContent(client, page, "First writer");

        var response = await client.PutAsJsonAsync($"api/v1/pages/{page.Id}/content", new SavePageRequest
        {
            Title = page.Title,
            Body = "Second writer",
            Revision = page.Revision,
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync(cancellationToken));

        var conflict = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(cancellationToken);

        conflict.IsSuccess.Should().BeFalse();
        conflict.Payload!.Body.Should().Be("First writer");
        conflict.Payload.Revision.Should().Be(page.Revision + 1);
    }

    [Fact]
    public async Task UpdatePage_ShouldSetTheStatusAndTagsByName()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Tagged page");
        var tag = await CreateTag(client);

        var response = await client.PatchAsJsonAsync($"api/v1/pages/{page.Id}", new
        {
            status = PageStatus.Published,
            tags = new[] { tag.Name.ToUpperInvariant() },
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var updated = await Read<PageViewModel>(response);

        updated.Status.Should().Be(PageStatus.Published);
        updated.Tags.Should().ContainSingle(item => item.Id == tag.Id);

        var clearResponse = await client.PatchAsJsonAsync($"api/v1/pages/{page.Id}", new
        {
            tags = Array.Empty<string>(),
        }, cancellationToken);

        var cleared = await Read<PageViewModel>(clearResponse);

        cleared.Tags.Should().BeEmpty();
        cleared.Status.Should().Be(PageStatus.Published);
    }

    [Fact]
    public async Task UpdatePage_ShouldReturnBadRequest_WhenATagDoesNotExist()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Unknown tag page");
        var missingTag = $"missing-{Guid.NewGuid():N}";

        var response = await client.PatchAsJsonAsync($"api/v1/pages/{page.Id}", new
        {
            tags = new[] { missingTag },
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(cancellationToken);

        result.Message.Should().Be($"Tags were not found in the workspace: {missingTag}.");
    }

    [Fact]
    public async Task UpdatePage_ShouldReturnBadRequest_WhenATagNameIsNull()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Null tag page");
        var content = new StringContent("""{"tags":[null]}""", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PatchAsync($"api/v1/pages/{page.Id}", content, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await Read<ClientResponse<PageViewModel>>(response);

        result.Message.Should().Be("Tags cannot be empty.");
    }

    [Fact]
    public async Task UpdatePage_ShouldForbidTags_WhenTheCredentialCannotAssignTags()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scopes = Permissions.Where(permission => permission != NetptunePermissions.Tags.Assign).ToArray();
        var apiKey = await CreateApiKey(scopes, scopes);
        var client = Fixture.CreateApiClient(apiKey);
        var page = await CreatePage(client, "Untaggable page");
        var tag = await CreateTag(client);

        var tagResponse = await client.PatchAsJsonAsync($"api/v1/pages/{page.Id}", new
        {
            tags = new[] { tag.Name },
        }, cancellationToken);
        var statusResponse = await client.PatchAsJsonAsync($"api/v1/pages/{page.Id}", new
        {
            status = PageStatus.Published,
        }, cancellationToken);

        tagResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK, await statusResponse.Content.ReadAsStringAsync(cancellationToken));
    }

    [Fact]
    public async Task MovePage_ShouldPlaceThePageUnderTheParent()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = await Create(client, new CreatePageRequest { Kind = PageKind.Folder, Title = "API folder" });
        var page = await CreatePage(client, "Page to move");

        var response = await client.PostAsJsonAsync($"api/v1/pages/{page.Id}/move", new MovePageRequest
        {
            ParentId = folder.Id,
            SortOrder = 5,
        }, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var moved = await Read<PageViewModel>(response);

        moved.ParentId.Should().Be(folder.Id);

        var cycleResponse = await client.PostAsJsonAsync($"api/v1/pages/{folder.Id}/move", new MovePageRequest
        {
            ParentId = page.Id,
            SortOrder = 1,
        }, cancellationToken);

        cycleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchivePage_ShouldListItAsArchived_AndRestoreShouldBringItBack()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Page to archive");

        var archiveResponse = await client.DeleteAsync($"api/v1/pages/{page.Id}", cancellationToken);

        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await archiveResponse.Content.ReadAsStringAsync(cancellationToken));

        var archivedLookup = await client.GetAsync($"api/v1/pages/{page.Id}", cancellationToken);

        archivedLookup.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var archived = await client.GetFromJsonAsync<List<ArchivedPageViewModel>>("api/v1/pages/archived", cancellationToken);

        archived.Should().Contain(item => item.Id == page.Id);

        var restoreResponse = await client.PostAsync($"api/v1/pages/{page.Id}/restore", null, cancellationToken);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK, await restoreResponse.Content.ReadAsStringAsync(cancellationToken));

        var restoredLookup = await client.GetAsync($"api/v1/pages/{page.Id}", cancellationToken);

        restoredLookup.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteArchivedPage_ShouldRemoveThePageForGood()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await CreatePage(client, "Page to delete");

        var liveDeleteResponse = await client.DeleteAsync($"api/v1/pages/{page.Id}/permanent", cancellationToken);

        liveDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "only an archived page can be deleted for good");

        await client.DeleteAsync($"api/v1/pages/{page.Id}", cancellationToken);

        var deleteResponse = await client.DeleteAsync($"api/v1/pages/{page.Id}/permanent", cancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleteResponse.Content.ReadAsStringAsync(cancellationToken));

        var deletedLookup = await client.GetAsync($"api/v1/pages/{page.Id}", cancellationToken);
        var deletedRestore = await client.PostAsync($"api/v1/pages/{page.Id}/restore", null, cancellationToken);

        deletedLookup.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deletedRestore.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var archived = await client.GetFromJsonAsync<List<ArchivedPageViewModel>>("api/v1/pages/archived", cancellationToken);

        archived.Should().NotContain(item => item.Id == page.Id);
    }

    [Fact]
    public async Task ArchivePage_ShouldForbid_WhenOnlyTheServiceAccountCanArchiveOthersPages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var memberPermissions = Permissions.Append(NetptunePermissions.Pages.DeleteAny).ToArray();
        var apiKey = await CreateApiKey(memberPermissions, Permissions);
        var client = Fixture.CreateApiClient(apiKey);
        var humanPage = await CreatePageInTheApp("Someone else's page");

        var response = await client.DeleteAsync($"api/v1/pages/{humanPage.Id}", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArchivePage_ShouldArchiveOthersPages_WhenTheCredentialCanArchiveAnyPage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scopes = Permissions.Append(NetptunePermissions.Pages.DeleteAny).ToArray();
        var apiKey = await CreateApiKey(scopes, scopes);
        var client = Fixture.CreateApiClient(apiKey);
        var humanPage = await CreatePageInTheApp("Page an admin credential archives");

        var response = await client.DeleteAsync($"api/v1/pages/{humanPage.Id}", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    [Fact]
    public async Task PageVersions_ShouldListAndRestoreAnEarlierVersion()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var page = await Create(client, new CreatePageRequest { Title = "Versioned page", Body = "Original body" });
        var saved = await SaveContent(client, page, "Replacement body");

        var versions = await client.GetFromJsonAsync<List<PageVersionSummaryViewModel>>(
            $"api/v1/pages/{page.Id}/versions",
            cancellationToken);

        versions.Should().HaveCount(2);

        var original = versions.Single(version => version.Revision == page.Revision);
        var version = await client.GetFromJsonAsync<PageVersionViewModel>(
            $"api/v1/pages/{page.Id}/versions/{original.Id}",
            cancellationToken);

        version!.Body.Should().Be("Original body");

        var staleResponse = await client.PostAsync(
            $"api/v1/pages/{page.Id}/versions/{original.Id}/restore?revision={page.Revision}",
            null,
            cancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var restoreResponse = await client.PostAsync(
            $"api/v1/pages/{page.Id}/versions/{original.Id}/restore?revision={saved.Revision}",
            null,
            cancellationToken);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK, await restoreResponse.Content.ReadAsStringAsync(cancellationToken));

        var restored = await Read<PageViewModel>(restoreResponse);

        restored.Body.Should().Be("Original body");
        restored.Revision.Should().Be(saved.Revision + 1);
    }

    [Fact]
    public async Task GetPageLinks_ShouldReturnOutgoingLinksAndBacklinks()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = await CreatePage(client, "Link target");
        var source = await Create(client, new CreatePageRequest
        {
            Title = "Link source",
            Body = $"See [[page:{target.Id}|the target]] for details.",
        });

        var outgoing = await client.GetFromJsonAsync<PageLinksViewModel>($"api/v1/pages/{source.Id}/links", cancellationToken);
        var incoming = await client.GetFromJsonAsync<PageLinksViewModel>($"api/v1/pages/{target.Id}/links", cancellationToken);

        outgoing!.Pages.Should().ContainSingle(item => item.Id == target.Id);
        incoming!.Backlinks.Should().Contain(item => item.PageId == source.Id);
    }

    [Fact]
    public async Task GetPageChildren_ShouldListTheChildrenInTreeOrder_AndReturnNotFoundForAMissingPage()
    {
        var client = await CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = await Create(client, new CreatePageRequest { Kind = PageKind.Folder, Title = $"API folder {Guid.NewGuid():N}" });
        var first = await Create(client, new CreatePageRequest { ParentId = folder.Id, Title = "First", Body = "Opening line." });
        var second = await Create(client, new CreatePageRequest { ParentId = folder.Id, Title = "Second" });

        var response = await client.GetFromJsonAsync<List<PageChildViewModel>>($"api/v1/pages/{folder.Id}/children", cancellationToken);
        var children = response!;

        children.Select(child => child.Id).Should().Equal(first.Id, second.Id);
        children[0].Excerpt.Should().Be("Opening line.");
        children[0].ReadMinutes.Should().Be(1);

        var missing = await client.GetAsync("api/v1/pages/999999999/children", cancellationToken);

        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPages_ShouldReturnForbidden_WhenTheCredentialCannotReadPages()
    {
        var scopes = new[] { NetptunePermissions.Projects.Read };
        var apiKey = await CreateApiKey(scopes, scopes);
        var client = Fixture.CreateApiClient(apiKey);

        var response = await client.GetAsync("api/v1/pages", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePage_ShouldBroadcastAPageEvent_SoOpenClientsSeeIt()
    {
        var client = await CreateClient();
        var connection = Fixture.ApiServices.GetRequiredService<IConnectionMultiplexer>();
        var channel = RedisChannel.Literal(IWorkspaceEventPublisher.ChannelName);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var queue = await connection.GetSubscriber().SubscribeAsync(channel);

        queue.OnMessage(message =>
        {
            var published = JsonSerializer.Deserialize<JsonElement>(message.Message.ToString());
            var scopes = published.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString());

            if (scopes.Contains(WorkspaceEventScopes.Page))
            {
                received.TrySetResult();
            }
        });

        try
        {
            await CreatePage(client, "Broadcast page");

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

            completed.Should().Be(received.Task, "creating a page should publish a page workspace event");
        }
        finally
        {
            await connection.GetSubscriber().UnsubscribeAsync(channel);
        }
    }

    private async Task<PageViewModel> CreatePage(HttpClient client, string title)
    {
        return await Create(client, new CreatePageRequest { Title = $"{title} {Guid.NewGuid():N}", Body = "Body" });
    }

    private static async Task<PageViewModel> Create(HttpClient client, CreatePageRequest request)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await client.PostAsJsonAsync("api/v1/pages", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(cancellationToken));

        return await Read<PageViewModel>(response);
    }

    private static async Task<PageViewModel> SaveContent(HttpClient client, PageViewModel page, string body)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new SavePageRequest { Title = page.Title, Body = body, Revision = page.Revision };
        var response = await client.PutAsJsonAsync($"api/v1/pages/{page.Id}/content", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        return await Read<PageViewModel>(response);
    }

    private static async Task<TagViewModel> CreateTag(HttpClient client)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new AddTagRequest { Tag = $"page-{Guid.NewGuid():N}"[..20] };
        var response = await client.PostAsJsonAsync("api/v1/tags", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(cancellationToken));

        return await Read<TagViewModel>(response);
    }

    private async Task<PageViewModel> CreatePageInTheApp(string title)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var appClient = Fixture.CreateNetptuneClient();
        var request = new CreatePageRequest { Title = $"{title} {Guid.NewGuid():N}" };
        var response = await appClient.PostAsJsonAsync("api/pages", request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(cancellationToken);

        return result.Payload!;
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);

        return body!;
    }

    private async Task<HttpClient> CreateClient()
    {
        if (ApiKey is null)
        {
            await SetupLock.WaitAsync(TestContext.Current.CancellationToken);

            try
            {
                ApiKey ??= await CreateApiKey(Permissions, Permissions);
            }
            finally
            {
                SetupLock.Release();
            }
        }

        return Fixture.CreateApiClient(ApiKey);
    }

    private async Task<string> CreateApiKey(string[] memberPermissions, string[] scopes)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Fixture.CreateNetptuneClient();
        var accountRequest = new CreateServiceAccountRequest
        {
            Name = $"Pages agent {Guid.NewGuid():N}",
            Description = "Created by the public API pages integration test.",
            Permissions = memberPermissions,
        };

        var accountResponse = await client.PostAsJsonAsync("api/service-accounts", accountRequest, cancellationToken);

        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK, await accountResponse.Content.ReadAsStringAsync(cancellationToken));

        var accountResult = await accountResponse.Content.ReadFromJsonAsync<ClientResponse<ServiceAccountViewModel>>(cancellationToken);
        var account = accountResult.Payload!;

        var credentialRequest = new CreateApiCredentialRequest { Name = "Pages credential", Scopes = scopes };
        var credentialResponse = await client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            credentialRequest,
            cancellationToken);

        credentialResponse.StatusCode.Should().Be(HttpStatusCode.OK, await credentialResponse.Content.ReadAsStringAsync(cancellationToken));

        var credentialResult = await credentialResponse.Content
            .ReadFromJsonAsync<ClientResponse<ApiCredentialCreatedViewModel>>(cancellationToken);

        return credentialResult.Payload!.Token;
    }
}
