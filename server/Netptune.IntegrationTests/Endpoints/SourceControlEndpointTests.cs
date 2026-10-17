using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Events;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;
using Netptune.TestData;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SourceControlEndpointTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public SourceControlEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Providers_ShouldListOnlyTheEnabledProviders()
    {
        var providers = await Client.GetFromJsonAsync<List<SourceControlProviderViewModel>>(
            "api/source-control/providers",
            TestContext.Current.CancellationToken);

        var provider = providers.Should().ContainSingle().Subject;

        provider.Provider.Should().Be(SourceControlProvider.GitLab);
        provider.Terminology.PullRequest.Should().Be("Merge request");
        provider.Capabilities.Should().Contain("listRepositories");
    }

    [Fact]
    public async Task BeginConnect_ShouldRedirectToTheProvider()
    {
        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/begin",
            new BeginSourceControlConnectRequest { Provider = SourceControlProvider.GitLab },
            TestContext.Current.CancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectStartViewModel>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Payload!.RedirectUrl.Should().StartWith("https://scm.test/connect");
    }

    [Fact]
    public async Task BeginConnect_ShouldRejectAProviderThatIsNotEnabled()
    {
        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/begin",
            new BeginSourceControlConnectRequest { Provider = SourceControlProvider.GitHub },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Connect_ShouldStoreTheConnectionAndListItsRepositoriesUnlinked()
    {
        var account = UniqueAccount();

        var connection = await Connect(account);
        var repos = await GetRepos(connection.Id);
        var connections = await GetConnections();

        connection.AccountLogin.Should().Be(account);
        connection.Status.Should().Be(ScmConnectionStatus.Active);
        connection.Capabilities.Should().Contain("listRepositories");
        connections.Should().ContainSingle(listed => listed.Id == connection.Id);
        repos.Select(repo => repo.FullName).Should().Equal($"{account}/api", $"{account}/web");
        repos.Should().OnlyContain(repo => !repo.IsLinked);
    }

    [Fact]
    public async Task Connect_ShouldReuseTheConnectionWhenTheSameAccountIsConnectedAgain()
    {
        var account = UniqueAccount();

        var first = await Connect(account);
        var second = await Connect(account);
        var connections = await GetConnections();

        second.Id.Should().Be(first.Id);
        connections.Count(connection => connection.AccountLogin == account).Should().Be(1);
    }

    [Fact]
    public async Task Connect_ShouldReturnBadRequestWhenTheProviderDeniesIt()
    {
        var response = await PostComplete(UniqueAccount(), TestSourceControlProvider.DeniedCode);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkingARepo_ShouldCountTowardsTheConnection()
    {
        var connection = await Connect(UniqueAccount());
        var repos = await GetRepos(connection.Id);

        var response = await Client.PutAsJsonAsync(
            $"api/source-control/repos/{repos[0].Id}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = true },
            TestContext.Current.CancellationToken);

        var linked = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlRepoViewModel>>(
            TestContext.Current.CancellationToken);

        var listed = (await GetConnections()).Single(listedConnection => listedConnection.Id == connection.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        linked.Payload!.IsLinked.Should().BeTrue();
        linked.Payload.LinkedAt.Should().NotBeNull();
        listed.LinkedRepoCount.Should().Be(1);
    }

    [Fact]
    public async Task Disconnect_ShouldRemoveTheConnectionAndUnlinkItsRepos()
    {
        var connection = await Connect(UniqueAccount());
        var repos = await GetRepos(connection.Id);

        await Client.PutAsJsonAsync(
            $"api/source-control/repos/{repos[0].Id}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = true },
            TestContext.Current.CancellationToken);

        var response = await Client.DeleteAsync(
            $"api/source-control/connections/{connection.Id}",
            TestContext.Current.CancellationToken);

        var connections = await GetConnections();
        var reposAfter = await Client.GetAsync(
            $"api/source-control/connections/{connection.Id}/repos",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        connections.Should().NotContain(listed => listed.Id == connection.Id);
        reposAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await context.SourceControlRepos
            .Where(repo => repo.ConnectionId == connection.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        stored.Should().OnlyContain(repo => !repo.IsLinked);

        var auditedKeys = await context.EventRecords
            .Where(record => record.SubjectType == "source-control-connection")
            .Where(record => record.SubjectId == connection.Id.ToString())
            .Select(record => record.EventKey)
            .ToListAsync(TestContext.Current.CancellationToken);

        auditedKeys.Should().BeEquivalentTo([EventKeys.SourceControlConnected, EventKeys.SourceControlDisconnected]);
    }

    [Fact]
    public async Task Sync_ShouldRefreshTheRepositoryListing()
    {
        var connection = await Connect(UniqueAccount());

        var response = await Client.PostAsync(
            $"api/source-control/connections/{connection.Id}/sync",
            null,
            TestContext.Current.CancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ScmRepoSyncResult>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Payload!.Refreshed.Should().Be(2);
        result.Payload.Added.Should().Be(0);
    }

    [Fact]
    public async Task Members_ShouldSeeConnectionsButNotManageThem()
    {
        var member = SeedData.Users.ElementAt(2);
        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);

        var list = await memberClient.GetAsync("api/source-control/connections", TestContext.Current.CancellationToken);
        var begin = await memberClient.PostAsJsonAsync(
            "api/source-control/connections/begin",
            new BeginSourceControlConnectRequest { Provider = SourceControlProvider.GitLab },
            TestContext.Current.CancellationToken);

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        begin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string UniqueAccount()
    {
        return $"group-{Guid.NewGuid():N}"[..20];
    }

    private async Task<HttpResponseMessage> PostComplete(string account, string code = "ok")
    {
        var request = new CompleteSourceControlConnectRequest
        {
            Provider = SourceControlProvider.GitLab,
            Parameters = new Dictionary<string, string>
            {
                ["account"] = account,
                ["code"] = code,
            },
        };

        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            request,
            TestContext.Current.CancellationToken);

        return response;
    }

    private async Task<SourceControlConnectionViewModel> Connect(string account)
    {
        var response = await PostComplete(account);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);

        return result.Payload!;
    }

    private async Task<List<SourceControlConnectionViewModel>> GetConnections()
    {
        var connections = await Client.GetFromJsonAsync<List<SourceControlConnectionViewModel>>(
            "api/source-control/connections",
            TestContext.Current.CancellationToken);

        return connections!;
    }

    private async Task<List<SourceControlRepoViewModel>> GetRepos(int connectionId)
    {
        var result = await Client.GetFromJsonAsync<ClientResponse<List<SourceControlRepoViewModel>>>(
            $"api/source-control/connections/{connectionId}/repos",
            TestContext.Current.CancellationToken);

        return result.Payload!;
    }
}
