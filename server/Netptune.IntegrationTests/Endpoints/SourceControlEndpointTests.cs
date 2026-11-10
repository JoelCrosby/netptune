using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;
using Netptune.Core.SourceControl.Connect;
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
    public async Task Providers_ShouldSayHowEachProviderConnects()
    {
        var json = await Client.GetStringAsync("api/source-control/providers", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);

        var provider = document.RootElement.EnumerateArray().Single();
        var connectMethod = provider.GetProperty("connectMethod").GetInt32();

        connectMethod.Should().Be((int)ScmConnectMethod.Redirect);
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
    public async Task Connect_ShouldStoreTheCredentialHintAndExpiryTheProviderReturns()
    {
        var account = UniqueAccount();
        var expiresAt = new DateTime(2027, 3, 31, 23, 59, 59, DateTimeKind.Utc);

        var connection = await Connect(account, CredentialParameters("abcd", expiresAt));
        var stored = await GetStoredConnection(connection.Id);

        stored.CredentialHint.Should().Be("abcd");
        stored.CredentialExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Connect_ShouldReplaceTheCredentialHintAndExpiryWhenTheSameAccountReconnects()
    {
        var account = UniqueAccount();
        var firstExpiry = new DateTime(2027, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var secondExpiry = new DateTime(2027, 9, 30, 23, 59, 59, DateTimeKind.Utc);

        var first = await Connect(account, CredentialParameters("abcd", firstExpiry));
        var second = await Connect(account, CredentialParameters("wxyz", secondExpiry));
        var stored = await GetStoredConnection(second.Id);
        var connections = await GetConnections();

        second.Id.Should().Be(first.Id);
        stored.CredentialHint.Should().Be("wxyz");
        stored.CredentialExpiresAt.Should().Be(secondExpiry);
        connections.Count(listed => listed.AccountLogin == account).Should().Be(1);
    }

    [Fact]
    public async Task Connect_ShouldReturnBadRequestWhenTheProviderDeniesIt()
    {
        var response = await PostComplete(UniqueAccount(), TestSourceControlProvider.DeniedCode);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Connect_ShouldCreateNothingWhenARequiredCheckFails()
    {
        var account = UniqueAccount();
        var checks = ChecksParameters(TestSourceControlProvider.FailingChecks);

        var completion = await Complete(account, checks);
        var connections = await GetConnections();

        completion.Connection.Should().BeNull();
        completion.Checks.Should().ContainSingle(check => check.Status == ScmConnectionCheckStatus.Failed && check.IsRequired);
        connections.Should().NotContain(connection => connection.AccountLogin == account);
    }

    [Fact]
    public async Task Connect_ShouldHoldBackAConnectionWithWarningsUntilTheyAreAccepted()
    {
        var account = UniqueAccount();
        var checks = ChecksParameters(TestSourceControlProvider.WarningChecks);

        var heldBack = await Complete(account, checks);
        var connectionsBefore = await GetConnections();
        var accepted = await Complete(account, checks, acceptWarnings: true);
        var stored = await GetStoredConnection(accepted.Connection!.Id);

        heldBack.Connection.Should().BeNull();
        heldBack.Checks.Should().ContainSingle(check => check.Status == ScmConnectionCheckStatus.Warning && !check.IsRequired);
        connectionsBefore.Should().NotContain(connection => connection.AccountLogin == account);
        accepted.Connection.Checks.Should().HaveCount(2);
        accepted.Connection.LastCheckedAt.Should().NotBeNull();
        stored.LastCheckedAt.Should().NotBeNull();
        stored.LastCheck!.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Connect_ShouldConnectAtOnceWhenEveryCheckPasses()
    {
        var completion = await Complete(UniqueAccount(), ChecksParameters(TestSourceControlProvider.PassingChecks));

        completion.Connection.Should().NotBeNull();
        completion.Connection!.CanCheck.Should().BeTrue();
        completion.Checks.Should().OnlyContain(check => check.Status == ScmConnectionCheckStatus.Passed);
    }

    [Fact]
    public async Task Check_ShouldRunTheChecksAndStoreTheResult()
    {
        var connection = await Connect(UniqueAccount());

        var response = await Client.PostAsync(
            $"api/source-control/connections/{connection.Id}/check",
            null,
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);
        var stored = await GetStoredConnection(connection.Id);
        var listed = (await GetConnections()).Single(listed => listed.Id == connection.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);
        result.Payload!.LastCheckedAt.Should().NotBeNull();
        result.Payload.Checks.Select(check => check.Key)
            .Should()
            .Equal(TestSourceControlProvider.RequiredCheckKey, TestSourceControlProvider.OptionalCheckKey);
        stored.LastCheckedAt.Should().NotBeNull();
        listed.Checks.Should().HaveCount(2);
        listed.Status.Should().Be(ScmConnectionStatus.Active);
    }

    [Fact]
    public async Task Check_ShouldNotOverwriteTheResultWhenTheTokenIsReplacedMidCheck()
    {
        var connection = await Connect(UniqueAccount());
        var replacedCredential = new byte[] { 1, 2, 3, 4 };

        TestSourceControlProvider.DuringCheck[connection.Id] = () => ReplaceCredential(connection.Id, replacedCredential);

        var response = await Client.PostAsync(
            $"api/source-control/connections/{connection.Id}/check",
            null,
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);
        var stored = await GetStoredConnection(connection.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result!.Message);
        result.Payload!.LastCheckedAt.Should().BeNull();
        result.Payload.Checks.Should().BeEmpty();
        stored.Credential.Should().Equal(replacedCredential);
        stored.LastCheckedAt.Should().BeNull();
        stored.LastCheck.Should().BeNull();
    }

    [Fact]
    public async Task Check_ShouldBeRefusedForMembersWhoCannotManageSourceControl()
    {
        var connection = await Connect(UniqueAccount());
        var member = SeedData.Users.ElementAt(2);
        var memberClient = Fixture.CreateNetptuneClientAs(member.Email);

        var response = await memberClient.PostAsync(
            $"api/source-control/connections/{connection.Id}/check",
            null,
            TestContext.Current.CancellationToken);
        var stored = await GetStoredConnection(connection.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        stored.LastCheckedAt.Should().BeNull();
    }

    [Fact]
    public async Task Check_ShouldRefuseAConnectionWhoseProviderHasNoChecks()
    {
        var connection = await Connect(UniqueAccount());
        var gitHubConnectionId = await AddGitHubConnection(connection.Id);

        var response = await Client.PostAsync(
            $"api/source-control/connections/{gitHubConnectionId}/check",
            null,
            TestContext.Current.CancellationToken);
        var listed = (await GetConnections()).Single(listed => listed.Id == gitHubConnectionId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        listed.CanCheck.Should().BeFalse();
    }

    [Fact]
    public async Task Connect_ShouldOfferOnlyTheAccountsTheWorkspaceHasNotConnected()
    {
        var connected = UniqueAccount();
        var offered = UniqueAccount();

        await Connect(connected);

        var request = new CompleteSourceControlConnectRequest
        {
            Provider = SourceControlProvider.GitLab,
            Parameters = new Dictionary<string, string>
            {
                [TestSourceControlProvider.ChooseParameter] = $"{connected},{offered}",
            },
        };

        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            request,
            TestContext.Current.CancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectCompletionViewModel>>(
            TestContext.Current.CancellationToken);

        var choice = result.Payload!.Choice!;
        var candidate = choice.Candidates.Should().ContainSingle().Subject;

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);
        result.Payload.Connection.Should().BeNull();
        choice.InstallUrl.Should().Be(TestSourceControlProvider.InstallUrl);
        candidate.AccountLogin.Should().Be(offered);
        candidate.CallbackParameters.Should().Contain("account", offered);
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
    public async Task UnlinkingARepo_ShouldStopCountingItTowardsTheConnection()
    {
        var connection = await Connect(UniqueAccount());
        var repos = await GetRepos(connection.Id);

        await SetLinked(Client, repos[0].Id, isLinked: true);
        var response = await SetLinked(Client, repos[0].Id, isLinked: false);

        var listed = (await GetConnections()).Single(listedConnection => listedConnection.Id == connection.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        listed.LinkedRepoCount.Should().Be(0);
    }

    [Fact]
    public async Task LinkingARepo_ShouldFailWhenTheConnectionCanNoLongerSeeIt()
    {
        var connection = await Connect(UniqueAccount());
        var repos = await GetRepos(connection.Id);

        await UpdateRepo(repos[0].Id, repo => repo.SetProperty(stored => stored.IsAccessible, false));

        var response = await SetLinked(Client, repos[0].Id, isLinked: true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Connect_ShouldReactivateARevokedConnection()
    {
        var account = UniqueAccount();
        var connection = await Connect(account);

        using (var scope = Fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            await context.SourceControlConnections
                .Where(stored => stored.Id == connection.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(stored => stored.Status, ScmConnectionStatus.Revoked),
                    TestContext.Current.CancellationToken);
        }

        var reconnected = await Connect(account);

        reconnected.Id.Should().Be(connection.Id);
        reconnected.Status.Should().Be(ScmConnectionStatus.Active);
    }

    [Fact]
    public async Task ConnectionRoutes_ShouldReturnNotFoundForAnotherWorkspacesConnection()
    {
        var foreignClient = await CreateOwnerClient("linux");
        var foreignConnection = await Connect(UniqueAccount(), foreignClient);
        var foreignRepoId = (await GetRepos(foreignConnection.Id, foreignClient))[0].Id;
        var token = TestContext.Current.CancellationToken;

        var repos = await Client.GetAsync($"api/source-control/connections/{foreignConnection.Id}/repos", token);
        var sync = await Client.PostAsync($"api/source-control/connections/{foreignConnection.Id}/sync", null, token);
        var link = await SetLinked(Client, foreignRepoId, isLinked: true);
        var disconnect = await Client.DeleteAsync($"api/source-control/connections/{foreignConnection.Id}", token);
        var check = await Client.PostAsync($"api/source-control/connections/{foreignConnection.Id}/check", null, token);

        repos.StatusCode.Should().Be(HttpStatusCode.NotFound);
        sync.StatusCode.Should().Be(HttpStatusCode.NotFound);
        link.StatusCode.Should().Be(HttpStatusCode.NotFound);
        disconnect.StatusCode.Should().Be(HttpStatusCode.NotFound);
        check.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await GetRepos(foreignConnection.Id, foreignClient)).Should().NotBeEmpty("the owning workspace still has it");
    }

    [Fact]
    public async Task Identities_ShouldAddListAndRemoveAManualMapping()
    {
        var username = UniqueAccount();

        var added = await AddIdentity(Client, username);
        var identity = (await added.Content.ReadFromJsonAsync<ClientResponse<SourceControlIdentityViewModel>>(
            TestContext.Current.CancellationToken))!.Payload!;

        var listed = await GetIdentities(Client);
        var removed = await Client.DeleteAsync(
            $"api/source-control/identities/{identity.Id}",
            TestContext.Current.CancellationToken);
        var afterRemove = await GetIdentities(Client);

        added.StatusCode.Should().Be(HttpStatusCode.OK);
        identity.Username.Should().Be(username);
        identity.IsVerified.Should().BeFalse();
        listed.Should().Contain(candidate => candidate.Id == identity.Id);
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        afterRemove.Should().NotContain(candidate => candidate.Id == identity.Id);
    }

    [Fact]
    public async Task Identities_ShouldRefuseAUsernameAnotherMemberHasMapped()
    {
        var username = UniqueAccount();
        var memberClient = Fixture.CreateNetptuneClientAs(SeedData.Users.ElementAt(2).Email);

        var first = await AddIdentity(memberClient, username);
        var second = await AddIdentity(Client, username.ToUpperInvariant());

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Identities_ShouldNotRemoveAnotherMembersMapping()
    {
        var memberClient = Fixture.CreateNetptuneClientAs(SeedData.Users.ElementAt(2).Email);
        var added = await AddIdentity(memberClient, UniqueAccount());
        var identity = (await added.Content.ReadFromJsonAsync<ClientResponse<SourceControlIdentityViewModel>>(
            TestContext.Current.CancellationToken))!.Payload!;

        var removed = await Client.DeleteAsync(
            $"api/source-control/identities/{identity.Id}",
            TestContext.Current.CancellationToken);

        removed.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Identities_ShouldRejectAProviderThatIsNotEnabled()
    {
        var response = await AddIdentity(Client, UniqueAccount(), SourceControlProvider.GitHub);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private static async Task<HttpResponseMessage> AddIdentity(
        HttpClient client,
        string username,
        SourceControlProvider provider = SourceControlProvider.GitLab)
    {
        var response = await client.PostAsJsonAsync(
            "api/source-control/identities",
            new AddSourceControlIdentityRequest { Provider = provider, Username = username },
            TestContext.Current.CancellationToken);

        return response;
    }

    private static async Task<List<SourceControlIdentityViewModel>> GetIdentities(HttpClient client)
    {
        var identities = await client.GetFromJsonAsync<List<SourceControlIdentityViewModel>>(
            "api/source-control/identities",
            TestContext.Current.CancellationToken);

        return identities!;
    }

    private static async Task<HttpResponseMessage> SetLinked(HttpClient client, int repoId, bool isLinked)
    {
        var response = await client.PutAsJsonAsync(
            $"api/source-control/repos/{repoId}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = isLinked },
            TestContext.Current.CancellationToken);

        return response;
    }

    private async Task UpdateRepo(
        int repoId,
        Func<UpdateSettersBuilder<SourceControlRepo>, UpdateSettersBuilder<SourceControlRepo>> update)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.SourceControlRepos
            .Where(repo => repo.Id == repoId)
            .ExecuteUpdateAsync(setters => update(setters), TestContext.Current.CancellationToken);
    }

    private async Task<HttpClient> CreateOwnerClient(string slug)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var ownerEmail = await context.Workspaces
            .AsNoTracking()
            .Where(workspace => workspace.Slug == slug)
            .Select(workspace => workspace.Owner!.Email)
            .SingleAsync(TestContext.Current.CancellationToken);

        return Fixture.CreateNetptuneClientAs(ownerEmail!, slug);
    }

    private async Task<HttpResponseMessage> PostComplete(
        string account,
        string code = "ok",
        HttpClient? client = null,
        Dictionary<string, string>? extraParameters = null)
    {
        var parameters = new Dictionary<string, string>(extraParameters ?? [])
        {
            ["account"] = account,
            ["code"] = code,
        };

        var request = new CompleteSourceControlConnectRequest
        {
            Provider = SourceControlProvider.GitLab,
            Parameters = parameters,
        };

        var response = await (client ?? Client).PostAsJsonAsync(
            "api/source-control/connections/complete",
            request,
            TestContext.Current.CancellationToken);

        return response;
    }

    private Task<SourceControlConnectionViewModel> Connect(string account, HttpClient? client = null)
    {
        return Connect(account, null, client);
    }

    private async Task<SourceControlConnectionViewModel> Connect(
        string account,
        Dictionary<string, string>? extraParameters,
        HttpClient? client = null)
    {
        var response = await PostComplete(account, client: client, extraParameters: extraParameters);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectCompletionViewModel>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);

        return result.Payload!.Connection!;
    }

    private async Task<SourceControlConnectCompletionViewModel> Complete(
        string account,
        Dictionary<string, string> extraParameters,
        bool acceptWarnings = false)
    {
        var parameters = new Dictionary<string, string>(extraParameters) { ["account"] = account, ["code"] = "ok" };
        var request = new CompleteSourceControlConnectRequest
        {
            Provider = SourceControlProvider.GitLab,
            Parameters = parameters,
            AcceptWarnings = acceptWarnings,
        };

        var response = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            request,
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectCompletionViewModel>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, result.Message);

        return result.Payload!;
    }

    private static Dictionary<string, string> ChecksParameters(string outcome)
    {
        return new Dictionary<string, string> { [TestSourceControlProvider.ChecksParameter] = outcome };
    }

    private async Task<int> AddGitHubConnection(int sameWorkspaceConnectionId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var existing = await GetStoredConnection(sameWorkspaceConnectionId);
        var account = UniqueAccount();

        var connection = new SourceControlConnection
        {
            WorkspaceId = existing.WorkspaceId,
            Provider = SourceControlProvider.GitHub,
            ExternalAccountId = account,
            AccountLogin = account,
            AccountType = ScmAccountType.Organisation,
            Status = ScmConnectionStatus.Active,
            OwnerId = existing.OwnerId,
            CreatedByUserId = existing.CreatedByUserId,
        };

        context.SourceControlConnections.Add(connection);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return connection.Id;
    }

    private static Dictionary<string, string> CredentialParameters(string hint, DateTime expiresAt)
    {
        return new Dictionary<string, string>
        {
            [TestSourceControlProvider.CredentialHintParameter] = hint,
            [TestSourceControlProvider.CredentialExpiresAtParameter] = expiresAt.ToString("O", CultureInfo.InvariantCulture),
        };
    }

    private async Task ReplaceCredential(int connectionId, byte[] credential)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await context.SourceControlConnections
            .Where(stored => stored.Id == connectionId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(stored => stored.Credential, credential),
                TestContext.Current.CancellationToken);
    }

    private async Task<SourceControlConnection> GetStoredConnection(int connectionId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var connection = await context.SourceControlConnections
            .AsNoTracking()
            .SingleAsync(stored => stored.Id == connectionId, TestContext.Current.CancellationToken);

        return connection;
    }

    private async Task<List<SourceControlConnectionViewModel>> GetConnections()
    {
        var connections = await Client.GetFromJsonAsync<List<SourceControlConnectionViewModel>>(
            "api/source-control/connections",
            TestContext.Current.CancellationToken);

        return connections!;
    }

    private async Task<List<SourceControlRepoViewModel>> GetRepos(int connectionId, HttpClient? client = null)
    {
        var result = await (client ?? Client).GetFromJsonAsync<ClientResponse<List<SourceControlRepoViewModel>>>(
            $"api/source-control/connections/{connectionId}/repos",
            TestContext.Current.CancellationToken);

        return result.Payload!;
    }
}
