using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.Search;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PageSearchEndpointTests
{
    private static readonly TimeSpan IndexTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public PageSearchEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Search_ShouldFindAPageByItsBody_WithItsFolderPath()
    {
        var term = UniqueTerm();
        var folder = await Create(new CreatePageRequest { Kind = PageKind.Folder, Title = $"Handbook {term}" });
        var pageRequest = new CreatePageRequest { Kind = PageKind.Document, ParentId = folder.Id, Title = "Runbook" };
        var page = await Create(pageRequest);

        await Save(page, $"Rotate the keys, then ping [[task:NETP-1|{term}keys]].");

        var hit = await ReindexAndWaitForPage($"{term}keys", page.Id);

        hit.Title.Should().Be("Runbook");
        hit.Url.Should().Be($"/netptune/pages/{page.Slug}");
        hit.Subtitle.Should().Be(folder.Title);
        hit.Metadata["snippet"]!.ToString().Should().Contain($"{term}keys");
    }

    [Fact]
    public async Task Search_ShouldLeavePagesOut_WhenTheMemberCannotReadPages()
    {
        var term = UniqueTerm();
        var page = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = $"Pricing {term}" });

        await ReindexAndWaitForPage(term, page.Id);

        var member = SeedData.Users.ElementAt(2);

        await using var revoked = await MemberPermissions.Revoke(Client, member.Id, NetptunePermissions.Pages.Read);

        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);
        var cancellationToken = TestContext.Current.CancellationToken;

        var everything = await memberClient.GetFromJsonAsync<SearchResponse>(
            $"api/search?q={term}",
            cancellationToken);
        var pagesOnly = await memberClient.GetFromJsonAsync<SearchResponse>(
            $"api/search?q={term}&types=pages",
            cancellationToken);

        everything!.Results.Should().NotContain(item => item.Type == "page");
        pagesOnly!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldIncludePagesInThePublicApi_WhenTheCredentialCanReadPages()
    {
        var term = UniqueTerm();
        var page = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = $"Onboarding {term}" });

        await ReindexAndWaitForPage(term, page.Id);

        var scopes = new[] { NetptunePermissions.Projects.Read, NetptunePermissions.Pages.Read };
        var apiClient = Fixture.CreateApiClient(await CreateApiKey(scopes));

        var result = await apiClient.GetFromJsonAsync<SearchResponse>(
            $"api/v1/search?q={term}",
            TestContext.Current.CancellationToken);

        result!.Results.Should().Contain(item => item.Type == "page" && item.Id == page.Id);
    }

    [Fact]
    public async Task Search_ShouldLeavePagesOutOfThePublicApi_WhenTheCredentialCannotReadPages()
    {
        var term = UniqueTerm();
        var page = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = $"Offboarding {term}" });

        await ReindexAndWaitForPage(term, page.Id);

        var scopes = new[] { NetptunePermissions.Projects.Read };
        var apiClient = Fixture.CreateApiClient(await CreateApiKey(scopes));

        var result = await apiClient.GetFromJsonAsync<SearchResponse>(
            $"api/v1/search?q={term}",
            TestContext.Current.CancellationToken);

        result!.Results.Should().NotContain(item => item.Type == "page");
    }

    private static string UniqueTerm()
    {
        return $"zq{Guid.NewGuid():N}"[..14];
    }

    private async Task<SearchResultViewModel> ReindexAndWaitForPage(string term, int pageId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var reindex = await Client.PostAsync("api/search/reindex", null, cancellationToken);

        reindex.EnsureSuccessStatusCode();

        var url = $"api/search?q={Uri.EscapeDataString(term)}";
        var deadline = DateTime.UtcNow + IndexTimeout;

        while (true)
        {
            var result = await Client.GetFromJsonAsync<SearchResponse>(url, cancellationToken);
            var hit = result!.Results.FirstOrDefault(item => item.Type == "page" && item.Id == pageId);

            if (hit is not null)
            {
                return hit;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Page {pageId} was not searchable for '{term}' within {IndexTimeout}.");
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task<PageViewModel> Create(CreatePageRequest request)
    {
        var response = await Client.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private async Task Save(PageViewModel page, string body)
    {
        var request = new SavePageRequest { Title = page.Title, Body = body, Revision = page.Revision };
        var response = await Client.PutAsJsonAsync($"api/pages/{page.Id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> CreateApiKey(string[] scopes)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountRequest = new CreateServiceAccountRequest
        {
            Name = $"Search agent {Guid.NewGuid():N}",
            Description = "Created by the page search integration test.",
            Permissions = scopes,
        };

        var accountResponse = await Client.PostAsJsonAsync("api/service-accounts", accountRequest, cancellationToken);
        var accountResult = await accountResponse.Content.ReadFromJsonAsync<ClientResponse<ServiceAccountViewModel>>(cancellationToken);
        var account = accountResult.Payload!;

        var credentialRequest = new CreateApiCredentialRequest { Name = "Page search credential", Scopes = scopes };
        var credentialResponse = await Client.PostAsJsonAsync(
            $"api/service-accounts/{account.Id}/credentials",
            credentialRequest,
            cancellationToken);
        var credentialResult = await credentialResponse.Content
            .ReadFromJsonAsync<ClientResponse<ApiCredentialCreatedViewModel>>(cancellationToken);

        return credentialResult.Payload!.Token;
    }
}
